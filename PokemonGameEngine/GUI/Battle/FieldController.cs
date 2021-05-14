using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Render;
using System;

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
    }

    internal sealed partial class BattleGUI
    {
        private readonly PkmnPosition[][] _positions;

        public BattleGUI(PBEBattleFormat format)
        {
            _positions = new PkmnPosition[2][];
            switch (format)
            {
                case PBEBattleFormat.Single:
                {
                    _positions[0] = new PkmnPosition[1]
                    {
                        new PkmnPosition(0.015f, 0.25f, 0.40f, 0.95f) // Center
                    };
                    _positions[1] = new PkmnPosition[1]
                    {
                        new PkmnPosition(0.10f, 0.015f, 0.73f, 0.51f) // Center
                    };
                    break;
                }
                case PBEBattleFormat.Double:
                {
                    _positions[0] = new PkmnPosition[2]
                    {
                        new PkmnPosition(0.015f, 0.25f, 0.25f, 0.92f), // Left
                        new PkmnPosition(0.295f, 0.27f, 0.58f, 0.96f) // Right
                    };
                    _positions[1] = new PkmnPosition[2]
                    {
                        new PkmnPosition(0.38f, 0.035f, 0.85f, 0.53f), // Left
                        new PkmnPosition(0.10f, 0.015f, 0.63f, 0.52f) // Right
                    };
                    break;
                }
                case PBEBattleFormat.Triple:
                {
                    _positions[0] = new PkmnPosition[3]
                    {
                        new PkmnPosition(0.015f, 0.25f, 0.12f, 0.96f), // Left
                        new PkmnPosition(0.295f, 0.27f, 0.38f, 0.89f), // Center
                        new PkmnPosition(0.575f, 0.29f, 0.7f, 0.94f) // Right
                    };
                    _positions[1] = new PkmnPosition[3]
                    {
                        new PkmnPosition(0.66f, 0.055f, 0.91f, 0.525f), // Left
                        new PkmnPosition(0.38f, 0.035f, 0.75f, 0.55f), // Center
                        new PkmnPosition(0.10f, 0.015f, 0.56f, 0.53f) // Right
                    };
                    break;
                }
                case PBEBattleFormat.Rotation:
                {
                    _positions[0] = new PkmnPosition[3]
                    {
                        new PkmnPosition(0.015f, 0.25f, 0.06f, 0.99f), // Left
                        new PkmnPosition(0.295f, 0.27f, 0.4f, 0.89f), // Center
                        new PkmnPosition(0.575f, 0.29f, 0.88f, 1.025f) // Right
                    };
                    _positions[1] = new PkmnPosition[3]
                    {
                        new PkmnPosition(0.66f, 0.055f, 0.97f, 0.48f), // Left
                        new PkmnPosition(0.38f, 0.035f, 0.75f, 0.55f), // Center
                        new PkmnPosition(0.10f, 0.015f, 0.5f, 0.49f) // Right
                    };
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        private bool ShouldUseKnownInfo(PBETrainer trainer)
        {
            const bool hideNonOwned = true;
            return trainer != _trainer && hideNonOwned;
        }
        private bool IsBackImage(PBETeam team)
        {
            byte? owner = _trainer?.Team.Id;
            return team.Id == 0 ? owner != 1 : owner == 1; // Spectators/replays view from team 0's perspective
        }

        internal PkmnPosition GetStuff(PBEBattlePokemon pkmn, PBEFieldPosition position)
        {
            int i;
            switch (_battle.BattleFormat)
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
                default: throw new ArgumentOutOfRangeException(nameof(_battle.BattleFormat));
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
            PkmnPosition pos = GetStuff(pkmn, pkmn.FieldPosition);
            UpdatePokemon(pkmn, pos, true, true);
            pos.InfoVisible = true;
            pos.PkmnVisible = true;
        }
        private void ShowWildPokemon(PBEBattlePokemon pkmn)
        {
            PkmnPosition pos = GetStuff(pkmn, pkmn.FieldPosition);
            UpdatePokemon(pkmn, pos, true, false); // Only set the info to visible because the sprite is already loaded and visible
            pos.InfoVisible = true;
        }
        private void HidePokemon(PBEBattlePokemon pkmn, PBEFieldPosition oldPosition)
        {
            PkmnPosition pos = GetStuff(pkmn, oldPosition);
            AnimatedImage img = pos.SPkmn.AnimImage;
            pos.InfoVisible = false;
            pos.PkmnVisible = false;
            img.IsPaused = true;
        }
        private void UpdatePokemon(PBEBattlePokemon pkmn, bool info, bool sprite)
        {
            PkmnPosition pos = GetStuff(pkmn, pkmn.FieldPosition);
            UpdatePokemon(pkmn, pos, info, sprite);
        }
        private void MovePokemon(PBEBattlePokemon pkmn, PBEFieldPosition oldPosition)
        {
            PkmnPosition pos = GetStuff(pkmn, oldPosition);
            pos.InfoVisible = false;
            pos.PkmnVisible = false;
            pos = GetStuff(pkmn, pkmn.FieldPosition);
            UpdatePokemon(pkmn, pos, true, true);
            pos.InfoVisible = true;
            pos.PkmnVisible = true;
        }
    }
}
