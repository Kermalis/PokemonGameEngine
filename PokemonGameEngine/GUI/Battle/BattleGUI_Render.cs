using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.R3D;
using Kermalis.PokemonGameEngine.Render.Shaders;
using Kermalis.PokemonGameEngine.Render.World;
using Kermalis.PokemonGameEngine.Trainer;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed partial class BattleGUI
    {
        public static readonly Size2D RenderSize = new(480, 270); // 16:9
        private readonly FrameBuffer _frameBuffer;
        private readonly FrameBuffer _dayTintFrameBuffer;

        private ITransition _transition;

        private readonly PkmnPosition[][] _positions;
        private readonly BattlePokemonParty[] _parties;
        private BattleSprite _trainerSprite;

        private Window _stringWindow;
        private StringPrinter _stringPrinter;
        private float _autoAdvanceTime;

        private readonly Camera _camera;
        private readonly ModelShader _modelShader;
        private readonly BattleSpriteShader _spriteShader;
        private readonly BattleSpriteMesh _spriteMesh;
        private readonly List<Model> _models;

        private readonly PointLight[] _testLights = new PointLight[ModelShader.MAX_LIGHTS]
        {
            new(new Vector3(-5, 3, -5), new Vector3(5.00f, 2.25f, 0.60f), new Vector3(1f, 0.01f, 0.002f)),
            new(new Vector3( 5, 3, -5), new Vector3(0.85f, 0.52f, 4.00f), new Vector3(1f, 0.01f, 0.002f)),
            new(new Vector3(-5, 3,  5), new Vector3(0.85f, 3.52f, 0.88f), new Vector3(1f, 0.01f, 0.002f)),
            new(new Vector3( 5, 3,  5), new Vector3(0.85f, 0.52f, 0.88f), new Vector3(1f, 0.01f, 0.002f)),
        };

        private BattleGUI(PBEBattle battle, IReadOnlyList<Party> trainerParties)
        {
            Battle = battle;
            _trainer = battle.Trainers[0]; // Set before ShouldUseKnownInfo()
            _positions = PkmnPosition.CreatePositions(battle.BattleFormat);
            _parties = new BattlePokemonParty[battle.Trainers.Count];
            for (int i = 0; i < battle.Trainers.Count; i++)
            {
                PBETrainer trainer = battle.Trainers[i];
                _parties[i] = new BattlePokemonParty(trainer.Party, trainerParties[i], IsBackImage(trainer.Team.Id), ShouldUseKnownInfo(trainer), this);
            }

            // Projection matrix used in BW2 battles:
            // It doesn't match pixel-perfect most likely because of the DS's rounding precision (12 fractional bits)
            // But it's pretty close, and positions look identical if you don't try to look for differences
            var projection = new Matrix4x4(3.20947265625f,              0f,           0f,  0f,
                                                       0f, 4.333251953125f,           0f,  0f,
                                                       0f,              0f, -1.00390625f, -1f,
                                                       0f,              0f, -2.00390625f,  0f);

            GL gl = Display.OpenGL;
            _camera = new Camera(_defaultPosition, projection); // cam pos doesn't matter here since we will set it later
            _modelShader = new ModelShader(gl);
            _spriteShader = new BattleSpriteShader(gl);
            _spriteMesh = new BattleSpriteMesh(gl);

            _frameBuffer = FrameBuffer.CreateWithColorAndDepth(RenderSize); // Gets used at InitFadeIn()
            _dayTintFrameBuffer = FrameBuffer.CreateWithColor(RenderSize);

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
            const float floorY = -0.20f;
            switch (f)
            {
                case PBEBattleFormat.Single:
                case PBEBattleFormat.Double:
                case PBEBattleFormat.Triple:
                    foePos = new Vector3(0, floorY, -15); allyPos = new(0, floorY, 3); break;
                case PBEBattleFormat.Rotation:
                    foePos = new Vector3(0, floorY, -17.25f); allyPos = new(0, floorY, 8f); break;
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
            _trainerSprite = new BattleSprite(new Vector2(0.03660f, 0.04800f), new Vector3(0.75f, 0.25f, -12.0f), true) // Same as 1v1 foe pos
            {
                AnimImage = img,
                MaskColor = Colors.Black3,
                MaskColorAmt = 1f
            };
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

        private void RenderBattle()
        {
            AnimatedImage.UpdateAll();

            GL gl = Display.OpenGL;

            // 3D
            gl.Enable(EnableCap.DepthTest);
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            gl.BlendEquation(BlendEquationModeEXT.FuncAddExt);
            gl.ClearColor(Colors.Black3);
#if DEBUG_BATTLE_WIREFRAME
            gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
#endif
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Set up shader
            _modelShader.Use(gl);

            uint ms = SDL2.SDL.SDL_GetTicks();
            float rad = ms % 5_000 / 5_000f * 360 * Utils.DegToRad;
            // Move Test Lights
            _testLights[0].Pos.X = MathF.Sin(rad) * 20 - 10;
            _testLights[0].Pos.Z = MathF.Cos(rad) * 20 - 10;
            _testLights[1].Pos.X = MathF.Cos(rad * 2) * 20 - 6;
            _testLights[1].Pos.Z = MathF.Sin(rad * 2) * 20 - 6;
            _modelShader.SetCamera(gl, _camera); // Set projection, view, and camera position
            _modelShader.SetLights(gl, _testLights);
            _modelShader.SetShineDamper(gl, 5f);
            _modelShader.SetReflectivity(gl, 0f);

            // Draw models
            for (int i = 0; i < _models.Count; i++)
            {
                Model m = _models[i];
                _modelShader.SetTransform(gl, m.GetTransformation());

                m.Render(_modelShader);
            }

            // Draw battle sprites
            _spriteShader.Use(gl);
            gl.ActiveTexture(TextureUnit.Texture0);
            Matrix4x4 view = _camera.CreateViewMatrix();
            // Render trainer sprite if there is one
            if (_trainerSprite is not null && _trainerSprite.IsVisible)
            {
                _trainerSprite.Render(gl, _spriteMesh, _spriteShader, _camera.Projection, view);
            }
            // Render pkmn
            foreach (PkmnPosition[] ps in _positions)
            {
                foreach (PkmnPosition p in ps)
                {
                    if (p.Sprite.IsVisible)
                    {
                        p.Sprite.Render(gl, _spriteMesh, _spriteShader, _camera.Projection, view);
                    }
                }
            }

            gl.Disable(EnableCap.Blend);
            gl.Disable(EnableCap.DepthTest);
#if DEBUG_BATTLE_WIREFRAME
            gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill); // Reset
#endif

            DayTint.Render(_dayTintFrameBuffer);

            // 2D HUD

            void RenderTeamInfo(int i)
            {
                foreach (PkmnPosition p in _positions[i])
                {
                    if (p.InfoVisible)
                    {
                        p.RenderMonInfo();
                    }
                }
            }
            RenderTeamInfo(1);
            RenderTeamInfo(0);

            if (_stringPrinter is not null)
            {
                _stringWindow.Render();
            }

#if DEBUG_BATTLE_CAMERAPOS
            _camera.PR.Debug_RenderPosition();
#endif
        }
    }
}
