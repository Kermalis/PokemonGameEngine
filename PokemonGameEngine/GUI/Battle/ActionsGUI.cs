using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    // TODO: Switches (party menu)
    // TODO: Switch-ins
    // TODO: Usable moves
    // TODO: Non-single battles
    // TODO: Targets
    internal sealed class ActionsGUI
    {
        private readonly BattleGUI _parent;
        private readonly SpritedBattlePokemonParty _party;
        private readonly SpritedBattlePokemon _pkmn;

        private bool _isShowingMoves = false;
        private FadeFromColorTransition _fadeFromTransition;
        private FadeToColorTransition _fadeToTransition;
        private PartyMenuGUI _partyMenuGUI;

        private int _selectedMove = 0;

        public ActionsGUI(BattleGUI parent, SpritedBattlePokemonParty party, SpritedBattlePokemon pkmn)
        {
            _parent = parent;
            _party = party;
            _pkmn = pkmn;
        }

        public void LogicTick()
        {
            if (_fadeFromTransition != null || _fadeToTransition != null)
            {
                return;
            }
            if (_partyMenuGUI != null)
            {
                _partyMenuGUI.LogicTick();
                return;
            }

            PBEBattlePokemon pkmn = _pkmn.Pkmn;
            PBEBattleMoveset moves = pkmn.Moves;

            // Handle selection input
            bool down = InputManager.IsPressed(Key.Down);
            bool up = InputManager.IsPressed(Key.Up);
            bool a = InputManager.IsPressed(Key.A);
            bool b = InputManager.IsPressed(Key.B);
            if (!down && !up && !a && !b)
            {
                return;
            }

            if (!_isShowingMoves)
            {
                if (down && _selectedMove < 2 - 1)
                {
                    _selectedMove++;
                }
                if (up && _selectedMove > 0)
                {
                    _selectedMove--;
                }
                if (a)
                {
                    switch (_selectedMove)
                    {
                        case 0:
                        {
                            _isShowingMoves = true;
                            _selectedMove = 0;
                            break;
                        }
                        case 1:
                        {
                            void FadeToTransitionEnded()
                            {
                                _fadeToTransition = null;
                                void OnPartyMenuGUIClosed()
                                {
                                    void FadeFromTransitionEnded()
                                    {
                                        _fadeFromTransition = null;
                                    }
                                    _fadeFromTransition = new FadeFromColorTransition(20, 0, FadeFromTransitionEnded);
                                    _partyMenuGUI = null;
                                }
                                _partyMenuGUI = new PartyMenuGUI(_party, OnPartyMenuGUIClosed);
                            }
                            _fadeToTransition = new FadeToColorTransition(20, 0, FadeToTransitionEnded);
                            break;
                        }
                    }
                }
                return;
            }

            if (b)
            {
                _isShowingMoves = false;
                _selectedMove = 0;
                return;
            }
            if (down && _selectedMove < PBESettings.DefaultNumMoves - 1)
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

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            if (_partyMenuGUI != null)
            {
                _partyMenuGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                return;
            }

            Font fontDefault = Font.Default;
            uint[] defaultWhite = Font.DefaultWhite;

            if (!_isShowingMoves)
            {
                fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * 0.8), (int)((bmpHeight * 0.7) - (bmpHeight * (1 * 0.06))), "Fight", defaultWhite);
                fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * 0.8), (int)((bmpHeight * 0.7) - (bmpHeight * (0 * 0.06))), "Pokémon", defaultWhite);

                // Draw selection arrow
                fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * 0.75), (int)((bmpHeight * 0.7) - (bmpHeight * ((2 - 1 - _selectedMove) * 0.06))), "→", defaultWhite);

                _fadeFromTransition?.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                _fadeToTransition?.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                return;
            }

            PBEBattlePokemon pkmn = _pkmn.Pkmn;
            PBEBattleMoveset moves = pkmn.Moves;

            // Draw moves
            for (int i = 0; i < PBESettings.DefaultNumMoves; i++)
            {
                PBEBattleMoveset.PBEBattleMovesetSlot slot = moves[moves.Count - 1 - i];
                fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * 0.8), (int)((bmpHeight * 0.7) - (bmpHeight * (i * 0.06))), PBELocalizedString.GetMoveName(slot.Move).English, defaultWhite);
            }

            // Draw selection arrow
            fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * 0.75), (int)((bmpHeight * 0.7) - (bmpHeight * ((PBESettings.DefaultNumMoves - 1 - _selectedMove) * 0.06))), "→", defaultWhite);
        }
    }
}
