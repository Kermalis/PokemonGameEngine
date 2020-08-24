using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.GUI.Transition;
using System;
using System.Linq;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    // TODO: Switches (party menu)
    // TODO: Switch-ins
    // TODO: Non-single battles
    // TODO: Targets
    internal sealed class ActionsGUI : IDisposable
    {
        private readonly BattleGUI _parent;
        private readonly SpritedBattlePokemonParty _party;
        private readonly SpritedBattlePokemon _pkmn;

        private bool _isShowingMoves = false;
        private FadeFromColorTransition _fadeFromTransition;
        private FadeToColorTransition _fadeToTransition;
        private PartyMenuGUI _partyMenuGUI;
        private readonly GUIChoices _fightChoices;
        private GUIChoices _moveChoices;

        public ActionsGUI(BattleGUI parent, SpritedBattlePokemonParty party, SpritedBattlePokemon sPkmn)
        {
            _parent = parent;
            _party = party;
            _pkmn = sPkmn;

            _fightChoices = new GUIChoices(0.8f, 0.7f, 0.06f,
                font: Font.Default, fontColors: Font.DefaultWhite, selectedColors: Font.DefaultSelected, disabledColors: Font.DefaultDisabled)
            {
                new GUIChoice("Fight", FightChoice)
            };
            PBEBattlePokemon pkmn = _pkmn.Pkmn;
            bool enabled = pkmn.CanSwitchOut(); // Cannot switch out or use item if TempLockedMove exists
            Action command = enabled ? PokemonChoice : (Action)null;
            _fightChoices.Add(new GUIChoice("Pokémon", command, isEnabled: enabled));
            command = enabled ? BagChoice : (Action)null;
            _fightChoices.Add(new GUIChoice("Bag", command, isEnabled: enabled));
            enabled = pkmn.Trainer.ActiveBattlersOrdered.First() == pkmn && pkmn.Trainer.IsFleeValid() is null; // Only first Pokémon can "select" run
            command = enabled ? RunChoice : (Action)null;
            _fightChoices.Add(new GUIChoice("Run", command, isEnabled: enabled));
        }

        private void FightChoice()
        {
            PBEBattlePokemon pkmn = _pkmn.Pkmn;
            // Check if there's a move we must use
            bool auto = false;
            if (pkmn.IsForcedToStruggle())
            {
                pkmn.TurnAction = new PBETurnAction(pkmn, PBEMove.Struggle, PBEBattleUtils.GetPossibleTargets(pkmn, pkmn.GetMoveTargets(PBEMove.Struggle))[0]);
                auto = true;
            }
            else if (pkmn.TempLockedMove != PBEMove.None)
            {
                pkmn.TurnAction = new PBETurnAction(pkmn, pkmn.TempLockedMove, pkmn.TempLockedTargets);
                auto = true;
            }
            if (auto)
            {
                _parent.ActionsLoop(false);
                return;
            }

            // Create move choices if it's not already created
            if (_moveChoices is null)
            {
                PBEBattleMoveset moves = pkmn.Moves;
                PBEMove[] usableMoves = pkmn.GetUsableMoves();
                _moveChoices = new GUIChoices(0.8f, 0.7f, 0.06f, backCommand: () => _isShowingMoves = false,
                    font: Font.Default, fontColors: Font.DefaultWhite, selectedColors: Font.DefaultSelected, disabledColors: Font.DefaultDisabled);
                for (int i = 0; i < PBESettings.DefaultNumMoves; i++)
                {
                    PBEBattleMoveset.PBEBattleMovesetSlot slot = moves[i];
                    PBEMove m = slot.Move;
                    string text = PBELocalizedString.GetMoveName(m).English;
                    bool enabled = Array.IndexOf(usableMoves, m) != -1;
                    Action command = enabled ? () => SelectMoveForTurn(m) : (Action)null;
                    _moveChoices.Add(new GUIChoice(text, command, isEnabled: enabled));
                }
            }

            // Show moves
            _isShowingMoves = true;
        }
        private void PokemonChoice()
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
        }
        private void BagChoice()
        {
            // Temporarily auto select
            PBEBattlePokemon pkmn = _pkmn.Pkmn;
            pkmn.TurnAction = new PBETurnAction(pkmn, PBEItem.PokeBall);
            _parent.ActionsLoop(false);
        }
        private void RunChoice()
        {
            _parent.Flee();
        }

        private void SelectMoveForTurn(PBEMove move)
        {
            PBEBattlePokemon pkmn = _pkmn.Pkmn;
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
            if (_isShowingMoves)
            {
                _moveChoices.HandleInputs();
                return;
            }
            _fightChoices.HandleInputs();
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            if (_partyMenuGUI != null)
            {
                _partyMenuGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                return;
            }

            if (!_isShowingMoves)
            {
                _fightChoices.Render(bmpAddress, bmpWidth, bmpHeight);

                _fadeFromTransition?.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                _fadeToTransition?.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                return;
            }

            _moveChoices.Render(bmpAddress, bmpWidth, bmpHeight);
        }

        public void Dispose()
        {
            _fightChoices.Dispose();
            _moveChoices?.Dispose();
        }
    }
}
