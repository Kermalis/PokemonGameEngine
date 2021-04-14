using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.GUI.Interactive;
//using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using System;
using System.Linq;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    // TODO: Switches (party menu)
    // TODO: Switch-ins
    internal sealed class ActionsGUI : IDisposable
    {
        private enum ActionsState : byte
        {
            ShowAll, // Fight bag run etc
            Moves, // Show move selection for _pkmn
            Party, // Show party menu
            Targets, // Show move targets selection
            FadeToParty,
            FadeFromParty,
        }

        private readonly BattleGUI _parent;
        private readonly SpritedBattlePokemonParty _party;
        private readonly PBEBattlePokemon _pkmn;

        private ActionsState _state;
        /*private FadeFromColorTransition _fadeFromTransition;
        private FadeToColorTransition _fadeToTransition;
        private PartyMenuGUI _partyMenuGUI;*/
        private TargetsGUI _targetsGUI;
        private readonly TextGUIChoices _fightChoices;
        private TextGUIChoices _moveChoices;

        public ActionsGUI(BattleGUI parent, SpritedBattlePokemonParty party, PBEBattlePokemon pkmn)
        {
            _parent = parent;
            _party = party;
            _pkmn = pkmn;

            _fightChoices = new TextGUIChoices(0.8f, 0.7f, 0.06f,
                font: Font.Default, fontColors: Font.DefaultWhite, selectedColors: Font.DefaultSelected, disabledColors: Font.DefaultDisabled)
            {
                BottomAligned = true
            };
            _fightChoices.Add(new TextGUIChoice("Fight", FightChoice));
            bool enabled = pkmn.CanSwitchOut(); // Cannot switch out or use item if TempLockedMove exists
            Action command = enabled ? PokemonChoice : (Action)null;
            _fightChoices.Add(new TextGUIChoice("Pokémon", command, isEnabled: enabled));
            command = enabled ? BagChoice : (Action)null;
            _fightChoices.Add(new TextGUIChoice("Bag", command, isEnabled: enabled));
            enabled = pkmn.Trainer.ActiveBattlersOrdered.First() == pkmn && pkmn.Trainer.IsFleeValid() is null; // Only first Pokémon can "select" run
            command = enabled ? RunChoice : (Action)null;
            _fightChoices.Add(new TextGUIChoice("Run", command, isEnabled: enabled));
        }

        private void FightChoice()
        {
            // Check if there's a move we must use
            bool auto = false;
            if (_pkmn.IsForcedToStruggle())
            {
                _pkmn.TurnAction = new PBETurnAction(_pkmn, PBEMove.Struggle, PBEBattleUtils.GetPossibleTargets(_pkmn, _pkmn.GetMoveTargets(PBEMove.Struggle))[0]);
                auto = true;
            }
            else if (_pkmn.TempLockedMove != PBEMove.None)
            {
                _pkmn.TurnAction = new PBETurnAction(_pkmn, _pkmn.TempLockedMove, _pkmn.TempLockedTargets);
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
                PBEBattleMoveset moves = _pkmn.Moves;
                PBEMove[] usableMoves = _pkmn.GetUsableMoves();
                _moveChoices = new TextGUIChoices(0.8f, 0.7f, 0.06f, backCommand: () => _state = ActionsState.ShowAll,
                    font: Font.Default, fontColors: Font.DefaultWhite, selectedColors: Font.DefaultSelected, disabledColors: Font.DefaultDisabled)
                {
                    BottomAligned = true
                };
                for (int i = 0; i < PkmnConstants.NumMoves; i++)
                {
                    PBEBattleMoveset.PBEBattleMovesetSlot slot = moves[i];
                    PBEMove m = slot.Move;
                    string text = PBEDataProvider.Instance.GetMoveName(m).English;
                    bool enabled = Array.IndexOf(usableMoves, m) != -1;
                    Action command = enabled ? () => SelectMoveForTurn(m) : (Action)null;
                    _moveChoices.Add(new TextGUIChoice(text, command, isEnabled: enabled));
                }
            }

            // Show moves
            _state = ActionsState.Moves;
        }
        private void PokemonChoice()
        {
            /*void FadeToTransitionEnded()
            {
                _fadeToTransition = null;
                void OnPartyMenuGUIClosed()
                {
                    void FadeFromTransitionEnded()
                    {
                        _fadeFromTransition = null;
                        _state = ActionsState.ShowAll;
                    }
                    _fadeFromTransition = new FadeFromColorTransition(20, 0, FadeFromTransitionEnded);
                    _state = ActionsState.FadeFromParty;
                    _partyMenuGUI = null;
                }
                _partyMenuGUI = new PartyMenuGUI(_party, OnPartyMenuGUIClosed);
                _state = ActionsState.Party;
            }
            _fadeToTransition = new FadeToColorTransition(20, 0, FadeToTransitionEnded);
            _state = ActionsState.FadeToParty;*/
        }
        private void BagChoice()
        {
            // Temporarily auto select
            _pkmn.TurnAction = new PBETurnAction(_pkmn, PBEItem.DuskBall);
            _parent.ActionsLoop(false);
        }
        private void RunChoice()
        {
            _parent.Flee();
        }

        private void SelectMoveForTurn(PBEMove move)
        {
            PBEMoveTarget possibleTargets = _pkmn.GetMoveTargets(move);
            if (_pkmn.Battle.BattleFormat == PBEBattleFormat.Single || _pkmn.Battle.BattleFormat == PBEBattleFormat.Rotation)
            {
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
                _pkmn.TurnAction = new PBETurnAction(_pkmn, move, targets);
                _parent.ActionsLoop(false);
            }
            else // Double / Triple
            {
                void TargetSelected()
                {
                    _parent.ActionsLoop(false);
                    _targetsGUI = null;
                    // no need to change state since this'll get disposed in actionsloop
                }
                void TargetCancelled()
                {
                    _state = ActionsState.Moves;
                    _targetsGUI = null;
                }
                _targetsGUI = new TargetsGUI(_pkmn, possibleTargets, move, _parent._spritedParties, TargetSelected, TargetCancelled);
                _state = ActionsState.Targets;
            }
        }

        public void LogicTick()
        {
            switch (_state)
            {
                /*case ActionsState.Party:
                {
                    _partyMenuGUI.LogicTick();
                    return;
                }*/
                case ActionsState.Moves:
                {
                    _moveChoices.HandleInputs();
                    return;
                }
                case ActionsState.ShowAll:
                {
                    _fightChoices.HandleInputs();
                    return;
                }
                case ActionsState.Targets:
                {
                    _targetsGUI.LogicTick();
                    return;
                }
            }
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            switch (_state)
            {
                /*case ActionsState.FadeToParty:
                {
                    _fightChoices.Render(bmpAddress, bmpWidth, bmpHeight);
                    _fadeToTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    return;
                }
                case ActionsState.Party:
                {
                    _partyMenuGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    return;
                }
                case ActionsState.FadeFromParty:
                {
                    _fightChoices.Render(bmpAddress, bmpWidth, bmpHeight);
                    _fadeFromTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    return;
                }*/
                case ActionsState.ShowAll:
                {
                    _fightChoices.Render(bmpAddress, bmpWidth, bmpHeight);
                    return;
                }
                case ActionsState.Moves:
                {
                    _moveChoices.Render(bmpAddress, bmpWidth, bmpHeight);
                    return;
                }
                case ActionsState.Targets:
                {
                    _targetsGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    return;
                }
            }
        }

        public void Dispose()
        {
            _fightChoices.Dispose();
            _moveChoices?.Dispose();
        }
    }
}
