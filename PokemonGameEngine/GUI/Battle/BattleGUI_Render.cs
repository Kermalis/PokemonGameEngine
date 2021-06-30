using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Trainer;
using Kermalis.PokemonGameEngine.UI;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed class PkmnPosition
    {
        public bool InfoVisible;
        public bool PkmnVisible;
        public SpritedBattlePokemon SPkmn;

        public readonly float BarX;
        public readonly float BarY;
        public readonly float MonX;
        public readonly float MonY;

        public PkmnPosition(float barX, float barY, float monX, float monY)
        {
            BarX = barX;
            BarY = barY;
            MonX = monX;
            MonY = monY;
        }

        public unsafe void RenderMon(uint* dst, int dstW, int dstH, bool ally)
        {
            SPkmn.Render(dst, dstW, dstH, MonX, MonY, ally);
        }
        public unsafe void RenderMonInfo(uint* dst, int dstW, int dstH)
        {
            SPkmn.InfoBarImg.DrawOn(dst, dstW, dstH, BarX, BarY);
        }
    }

    internal sealed partial class BattleGUI
    {
        private readonly Image _battleBackground;
        private FadeColorTransition _fadeTransition;

        private readonly PkmnPosition[][] _positions;
        public readonly SpritedBattlePokemonParty[] SpritedParties;
        private readonly SpriteList _sprites = new();

        private Window _stringWindow;
        private StringPrinter _stringPrinter;
        private int _autoAdvanceTimer;

        private ActionsGUI _actionsGUI;

        private BattleGUI(PBEBattle battle, IReadOnlyList<Party> trainerParties)
        {
            _positions = CreatePositions(battle.BattleFormat);
            _battleBackground = Image.LoadOrGet($"GUI.Battle.Background.BG_{battle.BattleTerrain}_{battle.BattleFormat}.png");
            SpritedParties = new SpritedBattlePokemonParty[battle.Trainers.Count];
            for (int i = 0; i < battle.Trainers.Count; i++)
            {
                PBETrainer trainer = battle.Trainers[i];
                SpritedParties[i] = new SpritedBattlePokemonParty(trainer.Party, trainerParties[i], IsBackImage(trainer.Team), ShouldUseKnownInfo(trainer), this);
            }
        }

        private static PkmnPosition[][] CreatePositions(PBEBattleFormat f)
        {
            var a = new PkmnPosition[2][];
            switch (f)
            {
                case PBEBattleFormat.Single:
                {
                    a[0] = new PkmnPosition[1]
                    {
                        new PkmnPosition(0.015f, 0.25f, 0.40f, 0.95f) // Center
                    };
                    a[1] = new PkmnPosition[1]
                    {
                        new PkmnPosition(0.10f, 0.015f, 0.73f, 0.51f) // Center
                    };
                    break;
                }
                case PBEBattleFormat.Double:
                {
                    a[0] = new PkmnPosition[2]
                    {
                        new PkmnPosition(0.015f, 0.25f, 0.25f, 0.92f), // Left
                        new PkmnPosition(0.295f, 0.27f, 0.58f, 0.96f) // Right
                    };
                    a[1] = new PkmnPosition[2]
                    {
                        new PkmnPosition(0.38f, 0.035f, 0.85f, 0.53f), // Left
                        new PkmnPosition(0.10f, 0.015f, 0.63f, 0.52f) // Right
                    };
                    break;
                }
                case PBEBattleFormat.Triple:
                {
                    a[0] = new PkmnPosition[3]
                    {
                        new PkmnPosition(0.015f, 0.25f, 0.12f, 0.96f), // Left
                        new PkmnPosition(0.295f, 0.27f, 0.38f, 0.89f), // Center
                        new PkmnPosition(0.575f, 0.29f, 0.7f, 0.94f) // Right
                    };
                    a[1] = new PkmnPosition[3]
                    {
                        new PkmnPosition(0.66f, 0.055f, 0.91f, 0.525f), // Left
                        new PkmnPosition(0.38f, 0.035f, 0.75f, 0.55f), // Center
                        new PkmnPosition(0.10f, 0.015f, 0.56f, 0.53f) // Right
                    };
                    break;
                }
                case PBEBattleFormat.Rotation:
                {
                    a[0] = new PkmnPosition[3]
                    {
                        new PkmnPosition(0.015f, 0.25f, 0.06f, 0.99f), // Left
                        new PkmnPosition(0.295f, 0.27f, 0.4f, 0.89f), // Center
                        new PkmnPosition(0.575f, 0.29f, 0.88f, 1.025f) // Right
                    };
                    a[1] = new PkmnPosition[3]
                    {
                        new PkmnPosition(0.66f, 0.055f, 0.97f, 0.48f), // Left
                        new PkmnPosition(0.38f, 0.035f, 0.75f, 0.55f), // Center
                        new PkmnPosition(0.10f, 0.015f, 0.5f, 0.49f) // Right
                    };
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(f));
            }
            return a;
        }

        public unsafe void FadeIn()
        {
            OverworldGUI.ProcessDayTint(true); // Catch up time
            // Trainer sprite
            if (Battle.BattleType == PBEBattleType.Trainer)
            {
                var img = new AnimatedImage(TrainerCore.GetTrainerClassResource(_trainerClass), true, isPaused: true);
                var sprite = new Sprite
                {
                    Image = img,
                    DrawMethod = Renderer.Sprite_DrawWithShadow,
                    X = Renderer.GetCoordinatesForCentering(Program.RenderWidth, img.Width, 0.73f),
                    Y = Renderer.GetCoordinatesForEndAlign(Program.RenderHeight, img.Height, 0.51f),
                    Priority = int.MaxValue, // TODO
                    Tag = SpriteData_TrainerGoAway.Tag
                };
                _sprites.Add(sprite);
            }
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeInBattle);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        private void OnFadeInFinished()
        {
            if (Battle.BattleType == PBEBattleType.Trainer)
            {
                ((AnimatedImage)_sprites.FirstWithTagOrDefault(SpriteData_TrainerGoAway.Tag).Image).IsPaused = false;
                AddMessage(string.Format("You are challenged by {0}!", Battle.Teams[1].CombinedName), DestroyTrainerSpriteAndBegin);
                _pauseBattleThread = false;
            }
            else
            {
                Begin();
            }
        }
        private void DestroyTrainerSpriteAndBegin()
        {
            Sprite s = _sprites.FirstWithTagOrDefault(SpriteData_TrainerGoAway.Tag);
            s.Data = new SpriteData_TrainerGoAway(1_000, s.X);
            s.RCallback = Sprite_TrainerGoAway;
            Begin();
        }

        public void SetMessageWindowVisibility(bool invisible)
        {
            _stringWindow.IsInvisible = invisible;
        }

        private unsafe void RCB_Fading(uint* dst, int dstW, int dstH)
        {
            RCB_RenderTick(dst, dstW, dstH);
            _fadeTransition.Render(dst, dstW, dstH);
        }
        public unsafe void RCB_RenderTick(uint* dst, int dstW, int dstH)
        {
            AnimatedImage.UpdateCurrentFrameForAll();
            _sprites.DoRCallbacks();
            _battleBackground.DrawSizedOn(dst, dstW, dstH, 0, 0, dstW, dstH);
            void DoTeam(int i, bool info)
            {
                foreach (PkmnPosition p in _positions[i])
                {
                    bool ally = i == 0;
                    if (info)
                    {
                        if (p.InfoVisible)
                        {
                            p.RenderMonInfo(dst, dstW, dstH);
                        }
                    }
                    else if (p.PkmnVisible)
                    {
                        p.RenderMon(dst, dstW, dstH, ally);
                    }
                }
            }
            DoTeam(1, false);
            DoTeam(0, false);

            _sprites.DrawAll(dst, dstW, dstH);

            if (Overworld.ShouldRenderDayTint())
            {
                DayTint.Render(dst, dstW, dstH);
            }

            DoTeam(1, true);
            DoTeam(0, true);

            if (_stringPrinter != null)
            {
                _stringWindow.Render(dst, dstW, dstH);
            }
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
        private void UpdatePokemon(PBEBattlePokemon pkmn, PkmnPosition pos, bool info, bool sprite)
        {
            SpritedBattlePokemon sPkmn = SpritedParties[pkmn.Trainer.Id][pkmn];
            if (info)
            {
                sPkmn.UpdateInfoBar();
            }
            if (sprite)
            {
                sPkmn.UpdateSprites(pos, false);
            }
            pos.SPkmn = sPkmn;
        }
        // pkmn.FieldPosition must be updated before calling these
        private void ShowPokemon(PBEBattlePokemon pkmn)
        {
            PkmnPosition pos = GetPkmnPosition(pkmn, pkmn.FieldPosition);
            UpdatePokemon(pkmn, pos, true, true);
            pos.InfoVisible = true;
            pos.PkmnVisible = true;
        }
        private void ShowWildPokemon(PBEBattlePokemon pkmn)
        {
            PkmnPosition pos = GetPkmnPosition(pkmn, pkmn.FieldPosition);
            UpdatePokemon(pkmn, pos, true, false); // Only set the info to visible because the sprite is already loaded and visible
            pos.InfoVisible = true;
        }
        private void HidePokemon(PBEBattlePokemon pkmn, PBEFieldPosition oldPosition)
        {
            PkmnPosition pos = GetPkmnPosition(pkmn, oldPosition);
            AnimatedImage img = pos.SPkmn.AnimImage;
            pos.InfoVisible = false;
            pos.PkmnVisible = false;
            img.IsPaused = true;
        }
        private void UpdatePokemon(PBEBattlePokemon pkmn, bool info, bool sprite)
        {
            PkmnPosition pos = GetPkmnPosition(pkmn, pkmn.FieldPosition);
            UpdatePokemon(pkmn, pos, info, sprite);
        }
        private void MovePokemon(PBEBattlePokemon pkmn, PBEFieldPosition oldPosition)
        {
            PkmnPosition pos = GetPkmnPosition(pkmn, oldPosition);
            pos.InfoVisible = false;
            pos.PkmnVisible = false;
            pos = GetPkmnPosition(pkmn, pkmn.FieldPosition);
            UpdatePokemon(pkmn, pos, true, true);
            pos.InfoVisible = true;
            pos.PkmnVisible = true;
        }
    }
}
