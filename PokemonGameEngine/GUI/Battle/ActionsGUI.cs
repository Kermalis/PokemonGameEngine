using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Pkmn;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.Fonts;
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
                font: Font.Default, textColors: FontColors.DefaultWhite_I, selectedColors: FontColors.DefaultYellow_O, disabledColors: FontColors.DefaultDisabled);
            _fightChoices.AddOne("Fight", FightChoice);
            bool enabled = pkmn.CanSwitchOut(); // Cannot switch out or use item if TempLockedMove exists
            Action command = enabled ? PokemonChoice : null;
            _fightChoices.AddOne("Pokémon", command, isEnabled: enabled);
            command = enabled ? BagChoice : null;
            _fightChoices.AddOne("Bag", command, isEnabled: enabled);
            // Only first Pokémon can "select" run
            enabled = pkmn.Battle.BattleType == PBEBattleType.Wild && pkmn.Trainer.ActiveBattlersOrdered.First() == pkmn && pkmn.Trainer.IsFleeValid(out _);
            command = enabled ? RunChoice : null;
            _fightChoices.AddOne("Run", command, isEnabled: enabled);
        }

        public void SetCallbackForFightChoices()
        {
            Game.Instance.SetCallback(CB_HandleFightChoices);
        }
        private void SetCallbackForMoveChoices()
        {
            Game.Instance.SetCallback(CB_HandleMoveChoices);
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
            if (_moveChoices is not null)
            {
                return;
            }
            PBEBattleMoveset moves = _pkmn.Moves;
            PBEMove[] usableMoves = _pkmn.GetUsableMoves();
            _moveChoices = new TextGUIChoices(0.75f, 0.75f, bottomAlign: true, backCommand: SetCallbackForFightChoices,
                font: Font.Default, textColors: FontColors.DefaultWhite_I, selectedColors: FontColors.DefaultYellow_O, disabledColors: FontColors.DefaultDisabled);
            for (int i = 0; i < PkmnConstants.NumMoves; i++)
            {
                PBEBattleMoveset.PBEBattleMovesetSlot slot = moves[i];
                PBEMove m = slot.Move;
                string text = PBEDataProvider.Instance.GetMoveName(m).English;
                bool enabled = Array.IndexOf(usableMoves, m) != -1;
                Action command = enabled ? () => SelectMoveForTurn(m) : null;
                _moveChoices.AddOne(text, command, isEnabled: enabled);
            }
        }

        private void FightChoice()
        {
            // Check if there's a move we must use
            if (TryUseForcedMove())
            {
                BattleGUI.Instance.ActionsLoop();
                return;
            }

            CreateMoveChoices();
            SetCallbackForMoveChoices();
        }
        private void PokemonChoice()
        {
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeToParty);
        }
        private void BagChoice()
        {
            // Temporarily auto select
            _pkmn.TurnAction = new PBETurnAction(_pkmn, PBEItem.DuskBall);
            BattleGUI.Instance.ActionsLoop();
        }
        private void RunChoice()
        {
            BattleGUI.Instance.Flee();
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
                    default: throw new Exception();
                }
                _pkmn.TurnAction = new PBETurnAction(_pkmn, move, targets);
                BattleGUI.Instance.ActionsLoop();
            }
            else // Double / Triple
            {
                void TargetSelected()
                {
                    BattleGUI.Instance.ActionsLoop();
                    _targetsGUI.Delete();
                    _targetsGUI = null;
                    // no need to change callbacks since this ActionsGUI will get disposed in ActionsLoop
                }
                void TargetCancelled()
                {
                    _targetsGUI.Delete();
                    _targetsGUI = null;
                    SetCallbackForMoveChoices();
                }
                _targetsGUI = new TargetsGUI(_pkmn, possibleTargets, move, BattleGUI.Instance.SpritedParties, TargetSelected, TargetCancelled);
                Game.Instance.SetCallback(CB_HandleTargetsSelection);
            }
        }

        private void OnPartyClosed()
        {
            OverworldGUI.UpdateDayTint(true); // Catch up time
            _fadeTransition = FadeFromColorTransition.FromBlackStandard();
            short result = Game.Instance.Save.Vars[Var.SpecialVar_Result];
            if (result == PartyGUI.NO_PKMN_CHOSEN)
            {
                Game.Instance.SetCallback(CB_FadeFromParty); // No selection, display actions
            }
            else
            {
                BattleGUI.Instance.ClearMessage();
                Game.Instance.SetCallback(CB_FadeFromPartyNoChoices);
            }
        }
        private void OnFadeFromPartyFinished()
        {
            _fadeTransition = null;
            short result = Game.Instance.Save.Vars[Var.SpecialVar_Result];
            BattleGUI.Instance.SetMessageWindowVisibility(false);
            if (result == PartyGUI.NO_PKMN_CHOSEN)
            {
                SetCallbackForFightChoices(); // No selection, display actions
            }
            else
            {
                PBEBattlePokemon p = _party.BattleParty[result];
                _pkmn.TurnAction = new PBETurnAction(_pkmn, p);
                BattleGUI.Instance.StandBy.Add(p);
                BattleGUI.Instance.ActionsLoop();
            }
        }

        private void CB_HandleMoveChoices()
        {
            OverworldGUI.UpdateDayTint(false);
            BattleGUI.Instance.RenderBattle();
            _moveChoices.Render();

            _moveChoices.HandleInputs();
        }
        private void CB_HandleTargetsSelection()
        {
            OverworldGUI.UpdateDayTint(false);
            BattleGUI.Instance.RenderBattle();
            _targetsGUI.Render();

            _targetsGUI.HandleInputs();
        }
        private void CB_FadeToParty()
        {
            OverworldGUI.UpdateDayTint(false);
            RenderFadingWithFightChoices();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            BattleGUI.Instance.SetMessageWindowVisibility(true);
            _ = new PartyGUI(_party, PartyGUI.Mode.BattleSwitchIn, OnPartyClosed);
        }
        private void CB_FadeFromParty()
        {
            OverworldGUI.UpdateDayTint(false);
            RenderFadingWithFightChoices();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            OnFadeFromPartyFinished();
        }
        private void CB_FadeFromPartyNoChoices()
        {
            OverworldGUI.UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            OnFadeFromPartyFinished();
        }
        private void CB_HandleFightChoices()
        {
            OverworldGUI.UpdateDayTint(false);
            RenderWithFightChoices();
            _fightChoices.HandleInputs();
        }

        private void RenderFading()
        {
            BattleGUI.Instance.RenderBattle();
            _fadeTransition.Render();
        }
        private void RenderFadingWithFightChoices()
        {
            BattleGUI.Instance.RenderBattle();
            _fightChoices.Render();
            _fadeTransition.Render();
        }
        private void RenderWithFightChoices()
        {
            BattleGUI.Instance.RenderBattle();
            _fightChoices.Render();
        }

        public void Dispose()
        {
            _fightChoices.Dispose();
            _moveChoices?.Dispose();
        }
    }
}
