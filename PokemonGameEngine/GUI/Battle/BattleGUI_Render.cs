using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.R3D;
using Kermalis.PokemonGameEngine.Render.World;
using Kermalis.PokemonGameEngine.Trainer;
using Kermalis.PokemonGameEngine.World;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed class PkmnPosition
    {
        public const int AllyTag = 1932;
        public const int FoeTag = 1933;

        public bool InfoVisible;
        /// <summary>This is separate from <see cref="Sprite.IsInvisible"/> because it can be true while the sprite isn't visible (dig for example)</summary>
        public bool PkmnVisible; // TODO: Use
        public SpritedBattlePokemon SPkmn;
        public readonly Sprite Sprite; // X and Y refer to center points

        public readonly RelPos2D BarPos;
        public readonly RelPos2D MonPos;

        public PkmnPosition(SpriteList s, float barX, float barY, float monX, float monY, bool ally)
        {
            Sprite = new Sprite { IsInvisible = true, Tag = ally ? AllyTag : FoeTag }; // TODO
            s.Add(Sprite);
            BarPos = new RelPos2D(barX, barY);
            MonPos = new RelPos2D(monX, monY);
            UpdateSpritePos(Display.RenderSize);
        }

        public void UpdateSpritePos(Size2D dstSize)
        {
            Sprite.Pos = MonPos.Absolute(dstSize);
        }
        public void UpdateAnimationSpeed(PBEBattlePokemon pkmn)
        {
            PBEStatus1 s = pkmn.Status1;
            var img = (AnimatedImage)Sprite.Image;
            if (s == PBEStatus1.Frozen)
            {
                img.IsPaused = true;
            }
            else
            {
                img.SpeedModifier = s == PBEStatus1.Paralyzed || s == PBEStatus1.Asleep || pkmn.HPPercentage <= 0.25f ? 2 : 1;
                img.IsPaused = false;
            }
        }

        public void RenderMonInfo()
        {
            SPkmn.InfoBarImg.Render(BarPos.Absolute());
        }
    }

    internal sealed partial class BattleGUI
    {
        private FadeColorTransition _fadeTransition;

        private readonly PkmnPosition[][] _positions;
        public readonly SpritedBattlePokemonParty[] SpritedParties;
        private readonly SpriteList _sprites = new();

        private Window _stringWindow;
        private StringPrinter _stringPrinter;
        private float _autoAdvanceTime;

        private ActionsGUI _actionsGUI;

        // 3D stuff
        private readonly Camera _camera;
        private readonly ModelShader _shader;
        private readonly List<Model> _models;

        private readonly PointLight[] _testLights = new PointLight[ModelShader.MAX_LIGHTS]
        {
            new(new(-5, 3, -5), new(10, 134f/255, 5), new Vector3(1f, 0.01f, 0.002f)),
            new(new(5, 3, -5), new(218f/255, 134f/255, 4), new Vector3(1f, 0.01f, 0.002f)),
            new(new(-5, 3, 5), new(218f/255, 134f/255, 226f/255), new Vector3(1f, 0.01f, 0.002f)),
            new(new(5, 3, 5), new(218f/255, 134f/255, 226f/255), new Vector3(1f, 0.01f, 0.002f)),
        };

        private BattleGUI(PBEBattle battle, IReadOnlyList<Party> trainerParties)
        {
            Battle = battle;
            Trainer = battle.Trainers[0]; // Set before ShouldUseKnownInfo()
            _positions = CreatePositions(battle.BattleFormat, _sprites);
            SpritedParties = new SpritedBattlePokemonParty[battle.Trainers.Count];
            for (int i = 0; i < battle.Trainers.Count; i++)
            {
                PBETrainer trainer = battle.Trainers[i];
                SpritedParties[i] = new SpritedBattlePokemonParty(trainer.Party, trainerParties[i], IsBackImage(trainer.Team), ShouldUseKnownInfo(trainer), this);
            }

            // Projection matrix
            /*const float FOV = 35;
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(Utils.DegreesToRadiansF(FOV),
                1,
                0.1f, 1_000f);*/
            // Used in BW2 battles:
            // aspect ratio: 4f/2.96275f or 1.3501f
            // fov: 25.98999f
            var projection = new Matrix4x4(3.2095f, 0, 0, 0,
                0, 4.3333f, 0, 0,
                0, 0, -1.0039f, -1,
                0, 0, -2.0039f, 0);

            _camera = new Camera(PositionRotation.Default, projection);
            _shader = new ModelShader(Display.OpenGL);

            // Terrain:
            GetTerrainPath(battle.BattleTerrain, out string bgPath, out string allyPath, out string foePath);
            _models = new()
            {
                new Model(bgPath),
                new Model(foePath)
                {
                    PR = new PositionRotation(new Vector3(0, 0, -15), Rotation.Default)
                },
                new Model(allyPath)
                {
                    PR = new PositionRotation(new(0, 0, 3), Rotation.Default)
                },
                // You'd add more models here if you wanted. File paths for now
            };
        }

        private static PkmnPosition[][] CreatePositions(PBEBattleFormat f, SpriteList s)
        {
            var a = new PkmnPosition[2][];
            switch (f)
            {
                case PBEBattleFormat.Single:
                {
                    a[0] = new PkmnPosition[1]
                    {
                        new PkmnPosition(s, 0.015f, 0.25f, 0.40f, 0.95f, true) // Center
                    };
                    a[1] = new PkmnPosition[1]
                    {
                        new PkmnPosition(s, 0.10f, 0.015f, 0.73f, 0.51f, false) // Center
                    };
                    break;
                }
                case PBEBattleFormat.Double:
                {
                    a[0] = new PkmnPosition[2]
                    {
                        new PkmnPosition(s, 0.015f, 0.25f, 0.25f, 0.92f, true), // Left
                        new PkmnPosition(s, 0.295f, 0.27f, 0.58f, 0.96f, true) // Right
                    };
                    a[1] = new PkmnPosition[2]
                    {
                        new PkmnPosition(s, 0.38f, 0.035f, 0.85f, 0.53f, false), // Left
                        new PkmnPosition(s, 0.10f, 0.015f, 0.63f, 0.52f, false) // Right
                    };
                    break;
                }
                case PBEBattleFormat.Triple:
                {
                    a[0] = new PkmnPosition[3]
                    {
                        new PkmnPosition(s, 0.015f, 0.25f, 0.12f, 0.96f, true), // Left
                        new PkmnPosition(s, 0.295f, 0.27f, 0.38f, 0.89f, true), // Center
                        new PkmnPosition(s, 0.575f, 0.29f, 0.7f, 0.94f, true) // Right
                    };
                    a[1] = new PkmnPosition[3]
                    {
                        new PkmnPosition(s, 0.66f, 0.055f, 0.91f, 0.525f, false), // Left
                        new PkmnPosition(s, 0.38f, 0.035f, 0.75f, 0.55f, false), // Center
                        new PkmnPosition(s, 0.10f, 0.015f, 0.56f, 0.53f, false) // Right
                    };
                    break;
                }
                case PBEBattleFormat.Rotation:
                {
                    a[0] = new PkmnPosition[3]
                    {
                        new PkmnPosition(s, 0.015f, 0.25f, 0.06f, 0.99f, true), // Left
                        new PkmnPosition(s, 0.295f, 0.27f, 0.4f, 0.89f, true), // Center
                        new PkmnPosition(s, 0.575f, 0.29f, 0.88f, 1.025f, true) // Right
                    };
                    a[1] = new PkmnPosition[3]
                    {
                        new PkmnPosition(s, 0.66f, 0.055f, 0.97f, 0.48f, false), // Left
                        new PkmnPosition(s, 0.38f, 0.035f, 0.75f, 0.55f, false), // Center
                        new PkmnPosition(s, 0.10f, 0.015f, 0.5f, 0.49f, false) // Right
                    };
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(f));
            }
            return a;
        }

        #region Terrain

        private static bool TerrainHasSpecificPlatforms(PBEBattleTerrain terrain)
        {
            switch (terrain)
            {
                case PBEBattleTerrain.Cave:
                case PBEBattleTerrain.Grass:
                    return true;
            }
            return false;
        }
        private static string GetTerrainName(PBEBattleTerrain terrain)
        {
            switch (terrain)
            {
                case PBEBattleTerrain.Cave: return "Cave";
                case PBEBattleTerrain.Grass: return "Grass";
                default: return "Dark";
            }
        }
        private static void GetTerrainPath(PBEBattleTerrain terrain, out string bgPath, out string allyPlatformPath, out string foePlatformPath)
        {
            string name = GetTerrainName(terrain);
            bool specificPlat = TerrainHasSpecificPlatforms(terrain);
            bgPath = string.Format("BattleBG\\{0}\\{0}.dae", name);
            string FormatPlatform(string team)
            {
                return string.Format("BattleBG\\Platform{0}{1}\\{0}{1}.dae", name, team);
            }
            if (specificPlat)
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
            OverworldGUI.UpdateDayTint(true); // Catch up time
            // Trainer sprite
            if (Battle.BattleType == PBEBattleType.Trainer)
            {
                var img = new AnimatedImage(TrainerCore.GetTrainerClassAsset(_trainerClass), isPaused: true);
                var sprite = new Sprite
                {
                    Image = img,
                    //DrawMethod = Renderer.Sprite_DrawWithShadow,
                    Pos = Pos2D.CenterXBottomY(0.73f, 0.51f, img.Size),
                    Priority = int.MaxValue, // TODO: Make a textured plane as well
                    Tag = SpriteData_TrainerGoAway.Tag
                };
                _sprites.Add(sprite);
            }
            _fadeTransition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInBattle);
        }
        private void OnFadeInFinished()
        {
            if (Battle.BattleType == PBEBattleType.Trainer)
            {
                ((AnimatedImage)_sprites.FirstWithTagOrDefault(SpriteData_TrainerGoAway.Tag).Image).IsPaused = false;
                SetMessage(string.Format("You are challenged by {0}!", Battle.Teams[1].CombinedName), DestroyTrainerSpriteAndBegin);
            }
            else
            {
                Begin();
            }
        }
        private void DestroyTrainerSpriteAndBegin()
        {
            Sprite s = _sprites.FirstWithTagOrDefault(SpriteData_TrainerGoAway.Tag);
            s.Data = new SpriteData_TrainerGoAway(1_000, s.Pos.X);
            s.Callback = Sprite_TrainerGoAway;
            Begin();
        }

        public void SetMessageWindowVisibility(bool invisible)
        {
            _stringWindow.IsInvisible = invisible;
        }

        private void RenderFading()
        {
            RenderBattle();
            _fadeTransition.Render();
        }

        public void RenderBattle()
        {
            // Tasks

            AnimatedImage.UpdateAll();
            _sprites.DoCallbacks();
            _renderTasks.RunTasks();

            // 3D
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

            // Set up shader
            _shader.Use(gl);

            uint ms = SDL2.SDL.SDL_GetTicks();
            float rad = ms % 5_000 / 5_000f * 360 * Utils.DegToRad;
            // Move Test Lights
            _testLights[0].Pos.X = MathF.Sin(rad) * 20 - 10;
            _testLights[0].Pos.Z = MathF.Cos(rad) * 20 - 10;
            _testLights[1].Pos.X = MathF.Cos(rad * 2) * 20 - 6;
            _testLights[1].Pos.Z = MathF.Sin(rad * 2) * 20 - 6;
            _shader.SetCamera(gl, _camera); // Set projection, view, and camera position
            _shader.SetLights(gl, _testLights);
            _shader.SetShineDamper(gl, 5f);
            _shader.SetReflectivity(gl, 0f);

            // Draw models
            for (int i = 0; i < _models.Count; i++)
            {
                Model m = _models[i];
                _shader.SetTransform(gl, m.GetTransformation());

                m.Render(_shader);
            }

            gl.Disable(EnableCap.Blend);
            gl.Disable(EnableCap.DepthTest);
#if DEBUG_BATTLE_WIREFRAME
            gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill); // Reset
#endif

            // 2D HUD

            _sprites.SortByPriority();
            _sprites.DrawAll();

            if (Overworld.ShouldRenderDayTint())
            {
                DayTint.Render();
            }

            void DoTeam(int i)
            {
                foreach (PkmnPosition p in _positions[i])
                {
                    if (p.InfoVisible)
                    {
                        p.RenderMonInfo();
                    }
                }
            }
            DoTeam(1);
            DoTeam(0);

            if (_stringPrinter is not null)
            {
                _stringWindow.Render();
            }

#if DEBUG_BATTLE_CAMERAPOS
            _camera.PR.Debug_RenderPosition();
#endif
        }

        private bool ShouldUseKnownInfo(PBETrainer trainer)
        {
            const bool hideNonOwned = true;
            return trainer != Trainer && hideNonOwned;
        }
        private bool IsBackImage(PBETeam team)
        {
            byte? owner = Trainer?.Team.Id;
            return team.Id == 0 ? owner != 1 : owner == 1; // Spectators/replays view from team 0's perspective
        }

        internal PkmnPosition GetPkmnPosition(PBEBattlePokemon pkmn, PBEFieldPosition position)
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
            return _positions[pkmn.Team.Id][i];
        }
        private void UpdatePokemon(PBEBattlePokemon pkmn, PkmnPosition pos, bool info, bool sprite,
            bool spriteImage, bool spriteImageIfSubstituted, bool spriteMini, bool spriteVisibility)
        {
            SpritedBattlePokemon sPkmn = SpritedParties[pkmn.Trainer.Id][pkmn];
            if (info)
            {
                sPkmn.UpdateInfoBar();
            }
            if (sprite)
            {
                sPkmn.UpdateSprites(pos, spriteImage, spriteImageIfSubstituted, spriteMini, spriteVisibility);
            }
            pos.SPkmn = sPkmn;
        }
        // pkmn.FieldPosition must be updated before calling these
        private void ShowPokemon(PBEBattlePokemon pkmn)
        {
            PkmnPosition pos = GetPkmnPosition(pkmn, pkmn.FieldPosition);
            UpdatePokemon(pkmn, pos, true, true, true, true, true, true);
            pos.InfoVisible = true;
            pos.PkmnVisible = true;
            pos.UpdateAnimationSpeed(pkmn);
        }
        private void ShowWildPokemon(PBEBattlePokemon pkmn)
        {
            PkmnPosition pos = GetPkmnPosition(pkmn, pkmn.FieldPosition);
            UpdatePokemon(pkmn, pos, true, false, false, false, false, false); // Only set the info to visible because the sprite is already loaded and visible
            pos.InfoVisible = true;
        }
        private void HidePokemon(PBEBattlePokemon pkmn, PBEFieldPosition oldPosition)
        {
            PkmnPosition pos = GetPkmnPosition(pkmn, oldPosition);
            var img = (AnimatedImage)pos.Sprite.Image;
            pos.InfoVisible = false;
            pos.PkmnVisible = false;
            img.IsPaused = true;
        }
        private void UpdatePokemon(PBEBattlePokemon pkmn, bool info, bool sprite,
            bool spriteImage, bool spriteImageIfSubstituted, bool spriteMini, bool spriteVisibility)
        {
            PkmnPosition pos = GetPkmnPosition(pkmn, pkmn.FieldPosition);
            UpdatePokemon(pkmn, pos, info, sprite, spriteImage, spriteImageIfSubstituted, spriteMini, spriteVisibility);
        }
        private void MovePokemon(PBEBattlePokemon pkmn, PBEFieldPosition oldPosition)
        {
            PkmnPosition pos = GetPkmnPosition(pkmn, oldPosition);
            pos.InfoVisible = false;
            pos.PkmnVisible = false;
            pos = GetPkmnPosition(pkmn, pkmn.FieldPosition);
            UpdatePokemon(pkmn, pos, true, true, true, true, true, true);
            pos.InfoVisible = true;
            pos.PkmnVisible = true;
        }
        private void UpdateAnimationSpeed(PBEBattlePokemon pkmn)
        {
            PkmnPosition pos = GetPkmnPosition(pkmn, pkmn.FieldPosition);
            pos.UpdateAnimationSpeed(pkmn);
        }
    }
}
