using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.R3D;
using Kermalis.PokemonGameEngine.Render.Shaders;
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
        public static readonly Size2D RenderSize = new(480, 270); // 16:9
        private const uint SHADOW_TEXTURE_SIZE = 512;
        private readonly FrameBuffer _frameBuffer;
        private readonly FrameBuffer _dayTintFrameBuffer;
        private readonly FrameBuffer _shadowFrameBuffer;

        private ITransition _transition;

        private readonly PkmnPosition[][] _positions;
        private readonly BattlePokemonParty[] _parties;
        private BattleSprite _trainerSprite;

        private Window _stringWindow;
        private StringPrinter _stringPrinter;
        private float _autoAdvanceTime;
        private bool _hudInvisible;

        public readonly Camera Camera;
        private readonly Matrix4x4 _shadowViewProjection;
        private readonly BattleModelShader _modelShader;
        private readonly BattleSpriteShader _spriteShader;
        private readonly BattleSpriteMesh _spriteMesh;
        private readonly List<Model> _models;

        private readonly PointLight[] _testLights = new PointLight[BattleModelShader.MAX_LIGHTS]
        {
            new(new Vector3(-5, 3, -5), new Vector3(5.00f, 2.25f, 0.60f), new Vector3(1f, 0.01f, 0.002f)),
            new(new Vector3( 5, 3, -5), new Vector3(0.85f, 0.52f, 4.00f), new Vector3(1f, 0.01f, 0.002f)),
            new(new Vector3(-5, 3,  5), new Vector3(0.85f, 3.52f, 0.88f), new Vector3(1f, 0.01f, 0.002f)),
            new(new Vector3( 5, 3,  5), new Vector3(0.85f, 0.52f, 0.88f), new Vector3(1f, 0.01f, 0.002f)),
        };

        private BattleGUI(PBEBattle battle, IReadOnlyList<Party> trainerParties)
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
            Camera = new Camera(DefaultCamPosition, projection); // cam pos doesn't matter here since we will set it later
            _modelShader = new BattleModelShader(gl);
            _spriteShader = new BattleSpriteShader(gl);
            _spriteMesh = new BattleSpriteMesh(gl);

            _frameBuffer = FrameBuffer.CreateWithColorAndDepth(RenderSize); // Gets used at InitFadeIn()
            _dayTintFrameBuffer = FrameBuffer.CreateWithColor(RenderSize);
            _shadowFrameBuffer = FrameBuffer.CreateWithColorAndDepth(new Size2D(SHADOW_TEXTURE_SIZE, SHADOW_TEXTURE_SIZE));

            InitShadows(gl, out _shadowViewProjection);

            // Set battle stuff now
            Battle = battle;
            _trainer = battle.Trainers[0]; // Set before ShouldUseKnownInfo()
            _positions = PkmnPosition.CreatePositions(battle.BattleFormat);
            _parties = new BattlePokemonParty[battle.Trainers.Count];
            for (int i = 0; i < battle.Trainers.Count; i++)
            {
                PBETrainer trainer = battle.Trainers[i];
                _parties[i] = new BattlePokemonParty(trainer.Party, trainerParties[i], IsBackImage(trainer.Team.Id), ShouldUseKnownInfo(trainer), this);
            }

            // Terrain:
            GetTerrainPaths(battle.BattleTerrain, battle.BattleFormat == PBEBattleFormat.Rotation,
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
        private static bool TerrainHasTeamSpecificPlatforms(PBEBattleTerrain t)
        {
            switch (t)
            {
                case PBEBattleTerrain.Cave:
                case PBEBattleTerrain.Grass:
                    return true;
            }
            return false;
        }
        private static string GetTerrainName(PBEBattleTerrain t)
        {
            switch (t)
            {
                case PBEBattleTerrain.Cave: return "Cave";
                case PBEBattleTerrain.Grass: return "Grass";
                default: return "Dark";
            }
        }
        private static void GetTerrainPaths(PBEBattleTerrain t, bool rotation, out string bgPath, out string allyPlatformPath, out string foePlatformPath)
        {
            string name = GetTerrainName(t);
            bgPath = string.Format("BattleBG\\{0}\\{0}.dae", name);
            if (rotation)
            {
                allyPlatformPath = foePlatformPath = @"BattleBG\PlatformRotation\Rotation.dae";
                return;
            }
            // Load specific platforms for non-rotation battles
            bool teamSpecificPlatforms = TerrainHasTeamSpecificPlatforms(t);
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
        private static Vector3 GetTrainerSpritePos(PBEBattleFormat f)
        {
            Vector3 pos;
            // Pos is same as 1v1 foe
            switch (f)
            {
                // Scale just happens to be similar for these 3, because of similar floor heights
                case PBEBattleFormat.Single:
                case PBEBattleFormat.Triple:
                    pos.Y = 0.02f; break;
                case PBEBattleFormat.Double:
                    pos.Y = 0.015f; break;
                case PBEBattleFormat.Rotation:
                    pos.Y = 0.5f; break; // TODO
                default: throw new ArgumentOutOfRangeException(nameof(f));
            }
            pos.X = 0.75f;
            pos.Z = -12.0f;
            return pos;
        }

        #endregion

        public void InitFadeIn()
        {
            _frameBuffer.Use();
            DayTint.CatchUpTime = true;
            // Trainer sprite
            if (Battle.BattleType == PBEBattleType.Trainer)
            {
                InitTrainerSpriteAtBattleStart();
            }
            _transition = new FadeFromColorTransition(1f, Colors.Black3);
            Game.Instance.SetCallback(CB_FadeInBattle);
        }
        private void InitTrainerSpriteAtBattleStart()
        {
            var img = new AnimatedImage(TrainerCore.GetTrainerClassAsset(_trainerClass), isPaused: true);
            _trainerSprite = new BattleSprite(GetTrainerSpritePos(Battle.BattleFormat), true)
            {
                MaskColor = Colors.Black3,
                MaskColorAmt = 1f
            };
            _trainerSprite.UpdateImage(img);
            var data = new TaskData_TrainerReveal();
            _tasks.Add(Task_TrainerReveal, 0, data: data);
        }
        private void OnFadeInFinished()
        {
            if (Battle.BattleType == PBEBattleType.Trainer)
            {
                SetMessage(string.Format("You are challenged by {0}!", Battle.Teams[1].CombinedName), InitTrainerSpriteFadeAwayAndBegin);
            }
            else
            {
                Begin();
            }
        }
        private void InitTrainerSpriteFadeAwayAndBegin()
        {
            var data = new TaskData_TrainerGoAway(_trainerSprite.Pos.Z);
            _tasks.Add(Task_TrainerGoAway, 0, data: data);
            Begin();
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

            Render_3D();
            if (!_hudInvisible)
            {
                Render_HUD();
            }
        }
        private void Render_3D()
        {
            GL gl = Display.OpenGL;
            gl.Enable(EnableCap.DepthTest);
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            gl.BlendEquation(BlendEquationModeEXT.FuncAddExt);
            gl.ClearColor(Colors.Black3);
#if DEBUG_BATTLE_WIREFRAME
            gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
#endif
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Render_3D_UpdateLights();

            Matrix4x4 camView = Camera.CreateViewMatrix();

            // Clear shadow buffer
            _shadowFrameBuffer.Use();
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            #region Draw sprites to shadow buffer

            _spriteShader.Use(gl);
            _spriteShader.SetOutputShadow(gl, true);
            gl.ActiveTexture(TextureUnit.Texture0);
            Render_3D_BattleSprites(s => s.RenderShadow(gl, _spriteMesh, _spriteShader, _shadowViewProjection, camView));

            #endregion

            #region Draw models (shadows will be placed on top of them now)

            _frameBuffer.Use();
            _modelShader.Use(gl);
            _modelShader.SetCamera(gl, Camera.Projection, camView, Camera.PR.Position);
            _modelShader.SetLights(gl, _testLights);

            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, _shadowFrameBuffer.ColorTexture.Value);
            gl.ActiveTexture(TextureUnit.Texture1);
            gl.BindTexture(TextureTarget.Texture2D, _shadowFrameBuffer.DepthTexture.Value);
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
            Render_3D_BattleSprites(s => s.Render(gl, _spriteMesh, _spriteShader, Camera.Projection, camView));

            #endregion

            gl.Disable(EnableCap.Blend);
            gl.Disable(EnableCap.DepthTest);
#if DEBUG_BATTLE_WIREFRAME
            gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill); // Reset
#endif

            DayTint.Render(_dayTintFrameBuffer);
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

#if DEBUG_BATTLE_CAMERAPOS
            Camera.PR.Debug_RenderPosition();
#endif
        }

        private void Render_3D_UpdateLights()
        {
            uint ms = SDL2.SDL.SDL_GetTicks();
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
    }
}
