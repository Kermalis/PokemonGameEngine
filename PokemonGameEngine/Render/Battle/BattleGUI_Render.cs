﻿using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.R3D;
using Kermalis.PokemonGameEngine.Render.Shaders.Battle;
using Kermalis.PokemonGameEngine.Render.Transitions;
using Kermalis.PokemonGameEngine.Render.World;
using Kermalis.PokemonGameEngine.Trainer;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Battle
{
    internal sealed partial class BattleGUI
    {
        public static readonly Vec2I RenderSize = new(480, 270); // 16:9
        private const int SHADOW_TEXTURE_SIZE = 512;

        private readonly FrameBuffer _frameBuffer;
        private readonly FrameBuffer _dayTintFrameBuffer;
        private readonly FrameBuffer _shadowFrameBuffer;

        private readonly PkmnPosition[][] _positions;
        private readonly BattlePokemonParty[] _parties;
        private BattleSprite _trainerSprite;

        private ITransition _transition;
        private bool _hudInvisible;

        private Window _stringWindow;
        private StringPrinter _stringPrinter;
        /// <summary>Allows or disallows the messages to continue after finishing.
        /// Example: The trainer challenge message cannot advance until the trainer is done animating</summary>
        private bool _canAdvanceMsg = true;
        private float _autoAdvanceTime;

        public readonly Camera Camera;
        private readonly Matrix4x4 _shadowViewProjection;
        private readonly BattleModelShader _modelShader;
        private readonly BattleSpriteShader _spriteShader;
        private readonly List<Model> _models;

        private readonly PointLight[] _testLights = new PointLight[BattleModelShader.MAX_LIGHTS]
        {
            new(new Vector3(-5, 3, -5), new Vector3(5.00f, 2.25f, 0.60f), new Vector3(1f, 0.01f, 0.002f)),
            new(new Vector3( 5, 3, -5), new Vector3(0.85f, 0.52f, 4.00f), new Vector3(1f, 0.01f, 0.002f)),
            new(new Vector3(-5, 3,  5), new Vector3(0.85f, 3.52f, 0.88f), new Vector3(1f, 0.01f, 0.002f)),
            new(new Vector3( 5, 3,  5), new Vector3(0.85f, 0.52f, 0.88f), new Vector3(1f, 0.01f, 0.002f)),
        };

        private BattleGUI(PBEBattle battle, BattleBackground bg, IReadOnlyList<Party> trainerParties)
        {
            Instance = this;

            // Projection matrix used in BW2 battles:
            // It doesn't match pixel-perfect most likely because of the DS's rounding precision (12 fractional bits)
            // But it's pretty close, and positions look identical if you don't try to look for differences
            var projection = new Matrix4x4(3.20947265625f,              0f,           0f,  0f,
                                                       0f, 4.333251953125f,           0f,  0f,
                                                       0f,              0f, -1.00390625f, -1f,
                                                       0f,              0f, -2.00390625f,  0f);

            GL gl = Display.OpenGL;
            Camera = new Camera(default, projection); // Cam position is set in ActuallyStartFadeIn()
            _modelShader = new BattleModelShader(gl);
            _spriteShader = new BattleSpriteShader(gl);

            _frameBuffer = new FrameBuffer().AddColorTexture(RenderSize).AddDepthTexture(RenderSize);
            _dayTintFrameBuffer = new FrameBuffer().AddColorTexture(RenderSize);
            var shadowSize = new Vec2I(SHADOW_TEXTURE_SIZE, SHADOW_TEXTURE_SIZE);
            _shadowFrameBuffer = new FrameBuffer().AddColorTexture(shadowSize).AddDepthTexture(shadowSize);

            InitShadows(gl, out _shadowViewProjection);

            // Set battle stuff now
            Battle = battle;
            _trainer = battle.Trainers[0]; // Set before ShouldUseKnownInfo()
            _positions = PkmnPosition.CreatePositions(battle.BattleFormat);
            _parties = new BattlePokemonParty[battle.Trainers.Count];
            for (int i = 0; i < battle.Trainers.Count; i++)
            {
                PBETrainer trainer = battle.Trainers[i];
                _parties[i] = new BattlePokemonParty(trainer.Party, trainerParties[i], IsBackImage(trainer.Team.Id), ShouldUseKnownInfo(trainer));
            }

            // Terrain:
            GetTerrainPaths(bg, battle.BattleFormat == PBEBattleFormat.Rotation,
                out string bgPath, out string allyPlatformPath, out string foePlatformPath);
            GetPlatformTransforms(battle.BattleFormat,
                out float platformScale, out Vector3 foePos, out Vector3 allyPos);
            _models = new()
            {
                new Model(bgPath),
                new Model(foePlatformPath)
                {
                    Scale = new Vector3(platformScale),
                    PR = new PositionRotation(foePos, Rotation.Default)
                },
                new Model(allyPlatformPath)
                {
                    Scale = new Vector3(platformScale),
                    PR = new PositionRotation(allyPos, Rotation.Default)
                },
                // You'd add more models here if you wanted
            };
        }

        #region Terrain

        private static void GetPlatformTransforms(PBEBattleFormat f, out float scale, out Vector3 foePos, out Vector3 allyPos)
        {
            switch (f)
            {
                case PBEBattleFormat.Single:
                case PBEBattleFormat.Rotation:
                    scale = 1f; break;
                case PBEBattleFormat.Double:
                    scale = 1.2f; break;
                case PBEBattleFormat.Triple:
                    scale = 1.5f; break;
                default: throw new ArgumentOutOfRangeException(nameof(f));
            }
            switch (f)
            {
                case PBEBattleFormat.Single:
                {
                    const float floorY = -0.20f;
                    foePos = new Vector3(0, floorY, -15);
                    allyPos = new(0, floorY, 3);
                    break;
                }
                case PBEBattleFormat.Double:
                {
                    const float floorY = -0.25f;
                    foePos = new Vector3(0, floorY, -15);
                    allyPos = new(0, floorY, 3);
                    break;
                }
                case PBEBattleFormat.Triple:
                {
                    const float floorY = -0.30f;
                    foePos = new Vector3(0, floorY, -15);
                    allyPos = new(0, floorY, 3);
                    break;
                }
                case PBEBattleFormat.Rotation:
                {
                    const float floorY = -0.20f;
                    foePos = new Vector3(0, floorY, -17.25f);
                    allyPos = new(0, floorY, 8f);
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(f));
            }
        }
        private static bool TerrainHasTeamSpecificPlatforms(BattleBackground bg)
        {
            switch (bg)
            {
                case BattleBackground.Cave:
                case BattleBackground.Grass_Plain:
                case BattleBackground.Grass_Tall:
                    return true;
            }
            return false;
        }
        private static string GetTerrainName(BattleBackground bg)
        {
            switch (bg)
            {
                case BattleBackground.Cave:
                    return "Cave";
                case BattleBackground.Grass_Plain:
                case BattleBackground.Grass_Tall:
                    return "Grass";
            }
            return "Dark";
        }
        private static string GetPlatformName(BattleBackground bg)
        {
            switch (bg)
            {
                case BattleBackground.Cave:
                    return "Cave";
                case BattleBackground.Grass_Plain:
                    return "Grass";
                case BattleBackground.Grass_Tall:
                    return "TallGrass";
            }
            return "Dark";
        }
        private static void GetTerrainPaths(BattleBackground bg, bool rotation, out string bgPath, out string allyPlatformPath, out string foePlatformPath)
        {
            string name = GetTerrainName(bg);
            bgPath = string.Format("BattleBG\\{0}\\{0}.dae", name);
            if (rotation)
            {
                allyPlatformPath = foePlatformPath = @"BattleBG\PlatformRotation\Rotation.dae";
                return;
            }
            // Load specific platforms for non-rotation battles
            bool teamSpecificPlatforms = TerrainHasTeamSpecificPlatforms(bg);
            name = GetPlatformName(bg);
            string FormatPlatform(string team)
            {
                return string.Format("BattleBG\\Platform{0}{1}\\{0}{1}.dae", name, team);
            }
            if (teamSpecificPlatforms)
            {
                allyPlatformPath = FormatPlatform("Ally");
                foePlatformPath = FormatPlatform("Foe");
            }
            else
            {
                allyPlatformPath = foePlatformPath = FormatPlatform(string.Empty);
            }
        }
        public static float GetFloorY(PBEBattleFormat f)
        {
            switch (f)
            {
                case PBEBattleFormat.Single:
                case PBEBattleFormat.Triple:
                    return 0.02f;
                case PBEBattleFormat.Double:
                    return 0.015f;
                case PBEBattleFormat.Rotation:
                    return 0.063f;
                default: throw new ArgumentOutOfRangeException(nameof(f));
            }
        }

        #endregion

        public void InitFadeIn()
        {
            Display.SetMinimumWindowSize(RenderSize);

            _tasks.Add(new BackTask(Task_RenderWhite, 0, data: new TaskData_RenderWhite()));
            Game.Instance.SetCallback(CB_RenderWhite); // Renders white for half a second so the transition isn't jarring
        }
        private void ActuallyStartFadeIn()
        {
            DayTint.CatchUpTime = true;

            if (Battle.BattleType == PBEBattleType.Trainer)
            {
                _canAdvanceMsg = false; // Don't advance message till trainer anim finishes

                // Create trainer sprite
                var img = new AnimatedImage(TrainerCore.GetTrainerClassAssetPath(_trainerClass), isPaused: true);
                _trainerSprite = new BattleSprite(new Vector3(0.75f, GetFloorY(Battle.BattleFormat), -13f), true)
                {
                    BlacknessAmt = 1f
                };
                _trainerSprite.UpdateImage(img);
                _tasks.Add(new BackTask(Task_TrainerReveal, 0, data: new TaskData_TrainerReveal()));

                Camera.PR = new PositionRotation(new Vector3(10f, 5f, -25f), new Rotation(-70, 15, 0));
                CreateCameraMotionTask(DefaultCamPosition, 2f, method: PositionRotationAnimator.Method.Smooth);
            }
            else // Wild
            {
                // Darken the sprites until the camera is done moving
                foreach (PkmnPosition p in _positions[1])
                {
                    p.Sprite.BlacknessAmt = TaskData_WildReveal.START_AMT;
                }

                Camera.PR = new PositionRotation(new Vector3(8.4f, 7f, 2.3f), new Rotation(-32f, 13.3f, 0f));
                var nextPos = new PositionRotation(new Vector3(6.85f, 7f, 4.55f), new Rotation(-22f, 13f, 0f));
                CreateCameraMotionTask(nextPos, CAM_SPEED_DEFAULT);
            }

            _transition = new FadeFromColorTransition(1f, Colors.White3);
            Game.Instance.SetCallback(CB_FadeInBattle);
        }
        private void OnFadeInFinished()
        {
            if (Battle.BattleType == PBEBattleType.Trainer)
            {
                void InitTrainerSpriteFadeAwayAndBegin()
                {
                    _tasks.Add(new BackTask(Task_TrainerGoAway, 0, data: new TaskData_TrainerGoAway(_trainerSprite.Pos.Z)));

                    Begin();
                }
                SetMessage(string.Format("You are challenged by {0}!", Battle.Teams[1].CombinedName), InitTrainerSpriteFadeAwayAndBegin);
            }
            else // Wild
            {
                foreach (PkmnPosition p in _positions[1])
                {
                    PlayCry(p.BattlePkmn.PBEPkmn);
                }
                _tasks.Add(new BackTask(Task_WildReveal, 0, data: new TaskData_WildReveal()));

                CreateCameraMotionTask(DefaultCamPosition, CAM_SPEED_DEFAULT, onFinished: Begin);
            }
        }

        private void SetMessageWindowVisibility(bool invisible)
        {
            _stringWindow.IsInvisible = invisible;
        }

        private bool ShouldUseKnownInfo(PBETrainer trainer)
        {
            const bool HIDE_NON_OWNED = true;
            return trainer != _trainer && HIDE_NON_OWNED;
        }
        private bool IsBackImage(byte teamId)
        {
            byte? owner = _trainer?.Team.Id;
            return teamId == 0 ? owner != 1 : owner == 1; // Spectators/replays view from team 0's perspective
        }
        public PkmnPosition GetPkmnPosition(byte teamId, PBEFieldPosition position)
        {
            int i;
            switch (Battle.BattleFormat)
            {
                case PBEBattleFormat.Single:
                {
                    i = 0;
                    break;
                }
                case PBEBattleFormat.Double:
                {
                    i = position == PBEFieldPosition.Left ? 0 : 1;
                    break;
                }
                case PBEBattleFormat.Triple:
                case PBEBattleFormat.Rotation:
                {
                    i = position == PBEFieldPosition.Left ? 0 : position == PBEFieldPosition.Center ? 1 : 2;
                    break;
                }
                default: throw new Exception();
            }
            return _positions[teamId][i];
        }

        private void InitShadows(GL gl, out Matrix4x4 shadowViewProjection)
        {
            var centerOfWorld = new Vector3(0f, 0f, -5f);
            // The light that creates our shadows doesn't actually emit light, it is just a position
            // If the light pos moved, such as with the daytime, this would all need to be updated every frame instead
            var fakeLightPos = new Vector3(7f, 10f, 20f);
            var lightDir = Vector3.Normalize(-fakeLightPos);
            // Do math to create a view matrix which is coming from the light with the center of the battle as the middle of our view
            float pitchRad = MathF.Acos(new Vector2(lightDir.X, lightDir.Z).Length());
            float yawDeg = MathF.Atan(lightDir.X / lightDir.Z) * Utils.RadToDeg;
            if (lightDir.Z > 0)
            {
                yawDeg -= 180;
            }
            Matrix4x4 shadowView = Matrix4x4.CreateTranslation(Vector3.Negate(centerOfWorld))
                * Matrix4x4.CreateRotationY(-yawDeg * Utils.DegToRad)
                * Matrix4x4.CreateRotationX(pitchRad);

            // Projection matrix is orthographic, now based around the fake light
            shadowViewProjection = shadowView * Matrix4x4.CreateOrthographic(30, 30, 0, 30);
            // The magic below converts from GL coords to relative coords (+0.5 * 0.5) so we can sample on the shadow FBO's textures
            Matrix4x4 magic = Matrix4x4.CreateScale(0.5f)
                * Matrix4x4.CreateTranslation(new Vector3(0.5f, 0.5f, 0.5f));

            _modelShader.Use(gl);
            _modelShader.SetShadowConversion(gl, shadowViewProjection * magic);
        }

        private void RenderBattleAndHUD()
        {
            AnimatedImage.UpdateAll();

            GL gl = Display.OpenGL;
            Render_3D(gl);
            if (!_hudInvisible)
            {
                Render_HUD();
            }
#if DEBUG_BATTLE_CAMERAPOS
            Camera.PR.Debug_RenderPosition();
#endif
        }
        private void Render_3D(GL gl)
        {
#if DEBUG_BATTLE_WIREFRAME
            gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
#endif
            gl.Enable(EnableCap.DepthTest);

            _frameBuffer.Use(gl);
            gl.ClearColor(Colors.Black3);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Render_3D_UpdateLights();

            Matrix4x4 camView = Camera.CreateViewMatrix();

            // Clear shadow buffer
            _shadowFrameBuffer.UseAndViewport(gl);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            #region Draw sprites to shadow buffer

            _spriteShader.Use(gl);
            _spriteShader.SetOutputShadow(gl, true);
            gl.ActiveTexture(TextureUnit.Texture0);
            Render_3D_BattleSprites(s => s.RenderShadow(gl, _spriteShader, _shadowViewProjection, camView));

            #endregion

            #region Draw models (shadows will be placed on top of them now)

            _frameBuffer.UseAndViewport(gl);
            _modelShader.Use(gl);
            _modelShader.SetCamera(gl, Camera.Projection, camView, Camera.PR.Position);
            _modelShader.SetLights(gl, _testLights);

            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, _shadowFrameBuffer.ColorTextures[0].Texture);
            gl.ActiveTexture(TextureUnit.Texture1);
            gl.BindTexture(TextureTarget.Texture2D, _shadowFrameBuffer.DepthTexture.Texture);
            for (int i = 0; i < _models.Count; i++)
            {
                Model m = _models[i];
                _modelShader.SetTransform(gl, m.GetTransformation());
                m.Render(_modelShader);
            }

            #endregion

            #region Draw battle sprites now on top of the terrain

            _spriteShader.Use(gl);
            _spriteShader.SetOutputShadow(gl, false);
            gl.ActiveTexture(TextureUnit.Texture0);
            Render_3D_BattleSprites(s => s.Render(gl, _spriteShader, Camera.Projection, camView));

            #endregion

            gl.Disable(EnableCap.DepthTest); // Re-disable DepthTest
#if DEBUG_BATTLE_WIREFRAME
            gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill); // Reset
#endif

            DayTint.Render(_frameBuffer, _dayTintFrameBuffer);
        }
        private void Render_HUD()
        {
            // Render info bars
            foreach (PkmnPosition[] ps in _positions)
            {
                foreach (PkmnPosition p in ps)
                {
                    if (p.InfoVisible)
                    {
                        p.RenderMonInfo();
                    }
                }
            }

            // Message
            if (_stringPrinter is not null)
            {
                _stringWindow.Render();
            }
        }

        private void Render_3D_UpdateLights()
        {
            uint ms = Display.SDL.GetTicks();
            float rad = ms % 5_000 / 5_000f * 360 * Utils.DegToRad;

            (float sin, float cos) = MathF.SinCos(rad);
            _testLights[0].Pos.X = Utils.Lerp(-20, 20, sin * 0.5f + 0.5f);
            _testLights[0].Pos.Z = Utils.Lerp(-20, 20, cos * 0.5f + 0.5f);

            (sin, cos) = MathF.SinCos(rad * 2);
            _testLights[1].Pos.X = Utils.Lerp(-6, 6, sin * 0.5f + 0.5f);
            _testLights[1].Pos.Z = Utils.Lerp(-6, 6, cos * 0.5f + 0.5f);
        }
        private void Render_3D_BattleSprites(Action<BattleSprite> action)
        {
            // Render trainer sprite if there is one
            if (_trainerSprite is not null && _trainerSprite.IsVisible)
            {
                action(_trainerSprite);
            }
            // Render pkmn
            foreach (PkmnPosition[] ps in _positions)
            {
                foreach (PkmnPosition p in ps)
                {
                    if (p.Sprite.IsVisible)
                    {
                        action(p.Sprite);
                    }
                }
            }
        }

        private void RenderWhite()
        {
            GL gl = Display.OpenGL;
            _frameBuffer.Use(gl);
            gl.ClearColor(Colors.White3);
            gl.Clear(ClearBufferMask.ColorBufferBit);
        }
    }
}
