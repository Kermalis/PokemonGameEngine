using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Pkmn;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using System;
using System.Linq;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    // TODO: Disallow switching out if it's invalid (trapping effects. Currently not in PBE though. Temp-locked moves would just auto select instead of giving the switch option)
    internal sealed class ActionsGUI : IDisposable
    {
        private readonly SpritedBattlePokemonParty _party;
        private readonly PBEBattlePokemon _pkmn;

        private FadeColorTransition _fadeTransition;
        private TargetsGUI _targetsGUI;
        private readonly TextGUIChoices _fightChoices;
        private TextGUIChoices _moveChoices;

        public ActionsGUI(SpritedBattlePokemonParty party, PBEBattlePokemon pkmn)
        {
            _party = party;
            _pkmn = pkmn;

            _fightChoices = new TextGUIChoices(0.75f, 0.75f, bottomAlign: true,
                font: Font.Default, fontColors: Font.DefaultWhite_I, selectedColors: Font.DefaultYellow_O, disabledColors: Font.DefaultDisabled);
            _fightChoices.Add(new TextGUIChoice("Fight", FightChoice));
            bool enabled = pkmn.CanSwitchOut(); // Cannot switch out or use item if TempLockedMove exists
            Action command = enabled ? PokemonChoice : null;
            _fightChoices.Add(new TextGUIChoice("Pokémon", command, isEnabled: enabled));
            command = enabled ? BagChoice : null;
            _fightChoices.Add(new TextGUIChoice("Bag", command, isEnabled: enabled));
            enabled = pkmn.Battle.BattleType == PBEBattleType.Wild && pkmn.Trainer.ActiveBattlersOrdered.First() == pkmn && pkmn.Trainer.IsFleeValid() is null; // Only first Pokémon can "select" run
            command = enabled ? RunChoice : null;
            _fightChoices.Add(new TextGUIChoice("Run", command, isEnabled: enabled));
        }

        public unsafe void SetCallbacksForAllChoices()
        {
            Game.Instance.SetCallback(CB_All);
            Game.Instance.SetRCallback(RCB_All);
        }
        private unsafe void SetCallbacksForMoves()
        {
            Game.Instance.SetCallback(CB_Moves);
            Game.Instance.SetRCallback(RCB_Moves);
        }

        private bool TryUseForcedMove()
        {
            if (_pkmn.IsForcedToStruggle())
            {
                _pkmn.TurnAction = new PBETurnAction(_pkmn, PBEMove.Struggle, PBEBattleUtils.GetPossibleTargets(_pkmn, _pkmn.GetMoveTargets(PBEMove.Struggle))[0]);
                return true;
            }
            else if (_pkmn.TempLockedMove != PBEMove.None)
            {
                _pkmn.TurnAction = new PBETurnAction(_pkmn, _pkmn.TempLockedMove, _pkmn.TempLockedTargets);
                return true;
            }
            return false;
        }
        private void CreateMoveChoices()
        {
            if (_moveChoices is null)
            {
                PBEBattleMoveset moves = _pkmn.Moves;
                PBEMove[] usableMoves = _pkmn.GetUsableMoves();
                _moveChoices = new TextGUIChoices(0.75f, 0.75f, bottomAlign: true, backCommand: SetCallbacksForAllChoices,
                    font: Font.Default, fontColors: Font.DefaultWhite_I, selectedColors: Font.DefaultYellow_O, disabledColors: Font.DefaultDisabled);
                for (int i = 0; i < PkmnConstants.NumMoves; i++)
                {
                    PBEBattleMoveset.PBEBattleMovesetSlot slot = moves[i];
                    PBEMove m = slot.Move;
                    string text = PBEDataProvider.Instance.GetMoveName(m).English;
                    bool enabled = Array.IndexOf(usableMoves, m) != -1;
                    Action command = enabled ? () => SelectMoveForTurn(m) : null;
                    _moveChoices.Add(new TextGUIChoice(text, command, isEnabled: enabled));
                }
            }
        }

        private unsafe void FightChoice()
        {
            // Check if there's a move we must use
            if (TryUseForcedMove())
            {
                BattleGUI.Instance.ActionsLoop(false);
                return;
            }

            CreateMoveChoices();
            SetCallbacksForMoves();
        }
        private unsafe void PokemonChoice()
        {
            _fadeTransition = new FadeToColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeToParty);
            Game.Instance.SetRCallback(RCB_FadingParty);
        }
        private void BagChoice()
        {
            // Temporarily auto select
            _pkmn.TurnAction = new PBETurnAction(_pkmn, PBEItem.DuskBall);
            BattleGUI.Instance.ActionsLoop(false);
        }
        private void RunChoice()
        {
            BattleGUI.Instance.Flee();
        }

        private unsafe void SelectMoveForTurn(PBEMove move)
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
                    default: throw new Exception();
                }
                _pkmn.TurnAction = new PBETurnAction(_pkmn, move, targets);
                BattleGUI.Instance.ActionsLoop(false);
            }
            else // Double / Triple
            {
                void TargetSelected()
                {
                    BattleGUI.Instance.ActionsLoop(false);
                    _targetsGUI = null;
                    // no need to change callbacks since this'll get disposed in actionsloop
                }
                void TargetCancelled()
                {
                    _targetsGUI = null;
                    SetCallbacksForMoves();
                }
                _targetsGUI = new TargetsGUI(_pkmn, possibleTargets, move, BattleGUI.Instance.SpritedParties, TargetSelected, TargetCancelled);
                Game.Instance.SetCallback(CB_Targets);
                Game.Instance.SetRCallback(RCB_Targets);
            }
        }

        private unsafe void OnPartyClosed()
        {
            OverworldGUI.ProcessDayTint(true); // Catch up time
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeFromParty);
            short result = Game.Instance.Save.Vars[Var.SpecialVar_Result];
            if (result == -1) // No selection, display actions
            {
                Game.Instance.SetRCallback(RCB_FadingParty);
            }
            else
            {
                BattleGUI.Instance.ClearMessage();
                Game.Instance.SetRCallback(RCB_FadingPartyNoChoices);
            }
        }

        private void CB_Moves()
        {
            OverworldGUI.ProcessDayTint(false);
            _moveChoices.HandleInputs();
        }
        private void CB_Targets()
        {
            OverworldGUI.ProcessDayTint(false);
            _targetsGUI.LogicTick();
        }
        private void CB_FadeToParty()
        {
            OverworldGUI.ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                BattleGUI.Instance.SetMessageWindowVisibility(true);
                _ = new PartyGUI(_party, PartyGUI.Mode.BattleSwitchIn, OnPartyClosed);
            }
        }
        private void CB_FadeFromParty()
        {
            OverworldGUI.ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                short result = Game.Instance.Save.Vars[Var.SpecialVar_Result];
                BattleGUI.Instance.SetMessageWindowVisibility(false);
                if (result == -1) // No selection, display actions
                {
                    SetCallbacksForAllChoices();
                }
                else
                {
                    PBEBattlePokemon p = _party.BattleParty[result];
                    _pkmn.TurnAction = new PBETurnAction(_pkmn, p);
                    BattleGUI.Instance.StandBy.Add(p);
                    BattleGUI.Instance.ActionsLoop(false);
                }
            }
        }
        private void CB_All()
        {
            OverworldGUI.ProcessDayTint(false);
            _fightChoices.HandleInputs();
        }

        private unsafe void RCB_Moves(uint* dst, int dstW, int dstH)
        {
            BattleGUI.Instance.RCB_RenderTick(dst, dstW, dstH);
            _moveChoices.Render(dst, dstW, dstH);
        }
        private unsafe void RCB_Targets(uint* dst, int dstW, int dstH)
        {
            BattleGUI.Instance.RCB_RenderTick(dst, dstW, dstH);
            _targetsGUI.RenderTick(dst, dstW, dstH);
        }
        private unsafe void RCB_FadingParty(uint* dst, int dstW, int dstH)
        {
            RCB_All(dst, dstW, dstH);
            _fadeTransition.Render(dst, dstW, dstH);
        }
        private unsafe void RCB_FadingPartyNoChoices(uint* dst, int dstW, int dstH)
        {
            BattleGUI.Instance.RCB_RenderTick(dst, dstW, dstH);
            _fadeTransition.Render(dst, dstW, dstH);
        }
        private unsafe void RCB_All(uint* dst, int dstW, int dstH)
        {
            BattleGUI.Instance.RCB_RenderTick(dst, dstW, dstH);
            _fightChoices.Render(dst, dstW, dstH);
        }

        public void Dispose()
        {
            _fightChoices.Dispose();
            _moveChoices?.Dispose();
        }
    }
}
