using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Util;
using System;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class ActionsGUI
    {
        private readonly BattleGUI _parent;
        private readonly PBEBattlePokemon _pkmn;
        private int _selectedMove = 0;

        public ActionsGUI(BattleGUI parent, PBEBattlePokemon pkmn)
        {
            _parent = parent;
            _pkmn = pkmn;
        }

        public void LogicTick()
        {
            PBEBattlePokemon pkmn = _pkmn;
            PBEBattleMoveset moves = pkmn.Moves;

            // Handle selection input
            bool down = InputManager.IsPressed(Key.Down);
            bool up = InputManager.IsPressed(Key.Up);
            bool a = InputManager.IsPressed(Key.A);
            if (down || up || a)
            {
                if (down && _selectedMove < moves.Count - 1)
                {
                    _selectedMove++;
                }
                if (up && _selectedMove > 0)
                {
                    _selectedMove--;
                }
                if (a)
                {
                    PBEMove move = moves[_selectedMove].Move;
                    PBEMoveTarget possibleTargets = pkmn.GetMoveTargets(move);
                    // Single battle only
                    PBETurnTarget targets;
                    switch (possibleTargets)
                    {
                        case PBEMoveTarget.All: targets = PBETurnTarget.AllyCenter | PBETurnTarget.FoeCenter; break;
                        case PBEMoveTarget.AllFoes:
                        case PBEMoveTarget.AllFoesSurrounding:
                        case PBEMoveTarget.AllSurrounding:
                        case PBEMoveTarget.RandomFoeSurrounding:
                        case PBEMoveTarget.SingleFoeSurrounding:
                        case PBEMoveTarget.SingleNotSelf:
                        case PBEMoveTarget.SingleSurrounding: targets = PBETurnTarget.FoeCenter; break;
                        case PBEMoveTarget.AllTeam:
                        case PBEMoveTarget.Self:
                        case PBEMoveTarget.SelfOrAllySurrounding:
                        case PBEMoveTarget.SingleAllySurrounding: targets = PBETurnTarget.AllyCenter; break;
                        default: throw new ArgumentOutOfRangeException(nameof(possibleTargets));
                    }
                    pkmn.TurnAction = new PBETurnAction(pkmn, move, targets);
                    _parent.ActionsLoop(false);
                }
            }
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight, Font font, uint[] fontColors)
        {
            PBEBattlePokemon pkmn = _pkmn;
            PBEBattleMoveset moves = pkmn.Moves;

            // Draw moves
            for (int i = 0; i < moves.Count; i++)
            {
                PBEBattleMoveset.PBEBattleMovesetSlot slot = moves[moves.Count - 1 - i];
                RenderUtil.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * 0.8), (int)((bmpHeight * 0.7) - (bmpHeight * (i * 0.06))), PBELocalizedString.GetMoveName(slot.Move).English, font, fontColors);
            }

            // Draw selection arrow
            RenderUtil.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * 0.75), (int)((bmpHeight * 0.7) - (bmpHeight * ((moves.Count - 1 - _selectedMove) * 0.06))), "→", font, fontColors);
        }
    }
}
