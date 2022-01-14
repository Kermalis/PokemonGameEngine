using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Pkmn;
using Kermalis.PokemonGameEngine.Render.Transitions;
using Kermalis.PokemonGameEngine.Render.World;
using System;

// TODO: Disallow switching out if it's invalid (trapping effects. Currently not in PBE though. Temp-locked moves would just auto select instead of giving the switch option)
namespace Kermalis.PokemonGameEngine.Render.Battle
{
    internal sealed partial class BattleGUI
    {
        public ActionsBuilder ActionsBuilder;

        private PBEBattlePokemon _curActionPkmn;
        private TargetsGUI _targetsGUI;
        private TextGUIChoices _allChoices;
        private TextGUIChoices _moveChoices;

        public void SubmitActions(PBETurnAction[] acts)
        {
            CreateCameraMotionTask(DefaultCamPosition, CAM_SPEED_FAST);
            ActionsBuilder = null;
            RemoveActionsGUI();
            Game.Instance.SetCallback(CB_RunTasksAndEvents);
            CreateBattleThread(() => _trainer.SelectActionsIfValid(out _, acts));
        }

        private void RemoveActionsGUI()
        {
            _allChoices?.Dispose();
            _allChoices = null;
            _moveChoices?.Dispose();
            _moveChoices = null;
        }
        private void SetCallback_HandleAllChoices()
        {
            Game.Instance.SetCallback(CB_HandleAllChoices);
        }

        private void CB_HandleAllChoices()
        {
            RunTasks();
            RenderBattleAndHUD();
            _allChoices.Render(RenderSize);
            _frameBuffer.BlitToScreen();

            _allChoices.HandleInputs();
        }
        private void CB_HandleTargetsSelection()
        {
            RunTasks();
            RenderBattleAndHUD();
            _targetsGUI.Render();
            _frameBuffer.BlitToScreen();

            _targetsGUI.HandleInputs();
        }

        public void NextAction(int index, PBEBattlePokemon pkmn)
        {
            _curActionPkmn = pkmn;
            RemoveActionsGUI(); // Delete prev pkmn's choices

            BattlePokemon bPkmn = GetBattlePokemon(pkmn);
            CreateCameraMotionTask(bPkmn.Pos.FocusPreTurn, CAM_SPEED_FAST);
            SetStaticMessage($"What will {pkmn.Nickname} do?", SetCallback_HandleAllChoices);
            Game.Instance.SetCallback(CB_RunTasksAndEvents); // Will run the message task which will set the above callback

            // Create choices
            _allChoices = new TextGUIChoices(0.75f, 0.75f, bottomAlign: true,
                font: Font.Default, textColors: FontColors.DefaultWhite_I, selectedColors: FontColors.DefaultYellow_O, disabledColors: FontColors.DefaultDisabled);

            // Add "Fight" for moves, always enabled
            _allChoices.AddOne("Fight", ActionsMenu_MovesChoice);

            // Pokémon menu for switch replacements
            bool enabled = pkmn.CanSwitchOut(); // Cannot switch out or use item if TempLockedMove exists
            Action command = enabled ? ActionsMenu_PartyChoice : null;
            _allChoices.AddOne("Pokémon", command, isEnabled: enabled);

            // Bag menu for item use, cannot use if TempLockedMove exists either, so reuse "enabled"
            command = enabled ? ActionsMenu_BagChoice : null;
            _allChoices.AddOne("Bag", command, isEnabled: enabled);

            // Only first Pokémon can "select" run, otherwise have back button
            if (index == 0)
            {
                enabled = pkmn.Battle.BattleType == PBEBattleType.Wild && pkmn.Trainer.IsFleeValid(out _);
                command = enabled ? ActionsMenu_RunChoice : null;
                _allChoices.AddOne("Run", command, isEnabled: enabled);
            }
            else
            {
                _allChoices.AddOne("Back", ActionsMenu_BackChoice); // Always enabled
                _allChoices.BackCommand = ActionsMenu_BackChoice;
            }
        }

        #region Moves Choice

        private void CB_HandleMoveChoices()
        {
            RunTasks();
            RenderBattleAndHUD();
            _moveChoices.Render(RenderSize);
            _frameBuffer.BlitToScreen();

            _moveChoices.HandleInputs();
        }

        private void ActionsMenu_MovesChoice()
        {
            // Check if there's a move we must use
            if (TryUseForcedMove())
            {
                return; // Return if it was chosen
            }

            TryCreateMoveChoices();
            Game.Instance.SetCallback(CB_HandleMoveChoices);
        }
        private bool TryUseForcedMove()
        {
            PBEBattlePokemon p = _curActionPkmn;
            if (p.IsForcedToStruggle())
            {
                ActionsBuilder.PushMove(PBEMove.Struggle, PBEBattleUtils.GetPossibleTargets(p, p.GetMoveTargets(PBEMove.Struggle))[0]);
                return true;
            }
            else if (p.TempLockedMove != PBEMove.None)
            {
                ActionsBuilder.PushMove(p.TempLockedMove, p.TempLockedTargets);
                return true;
            }
            return false;
        }
        private void TryCreateMoveChoices()
        {
            if (_moveChoices is not null)
            {
                return;
            }

            PBEBattleMoveset moves = _curActionPkmn.Moves;
            PBEMove[] usableMoves = _curActionPkmn.GetUsableMoves();

            _moveChoices = new TextGUIChoices(0.75f, 0.75f, bottomAlign: true, backCommand: SetCallback_HandleAllChoices,
                font: Font.Default, textColors: FontColors.DefaultWhite_I, selectedColors: FontColors.DefaultYellow_O, disabledColors: FontColors.DefaultDisabled);
            for (int i = 0; i < PkmnConstants.NumMoves; i++)
            {
                PBEBattleMoveset.PBEBattleMovesetSlot slot = moves[i];
                PBEMove m = slot.Move;
                string text = PBEDataProvider.Instance.GetMoveName(m).English;

                bool enabled = Array.IndexOf(usableMoves, m) != -1;
                Action command = enabled ? () => UserSelectedMove(m) : null;
                _moveChoices.AddOne(text, command, isEnabled: enabled);
            }
        }

        private void UserSelectedMove(PBEMove move)
        {
            PBEMoveTarget possibleTargets = _curActionPkmn.GetMoveTargets(move);
            if (_curActionPkmn.Battle.BattleFormat is PBEBattleFormat.Single or PBEBattleFormat.Rotation)
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
                ActionsBuilder.PushMove(move, targets);
            }
            else // Double / Triple
            {
                void TargetSelected()
                {
                    _targetsGUI.Delete();
                    _targetsGUI = null;
                    // No need to change callbacks, it is set in NextAction() or SubmitActions()
                }
                void TargetCancelled()
                {
                    _targetsGUI.Delete();
                    _targetsGUI = null;
                    Game.Instance.SetCallback(CB_HandleMoveChoices);
                }
                _targetsGUI = new TargetsGUI(_curActionPkmn, possibleTargets, move, TargetSelected, TargetCancelled);
                Game.Instance.SetCallback(CB_HandleTargetsSelection);
            }
        }

        #endregion

        #region Party Menu Choice

        private void CB_FadeToPartyForBrowsing()
        {
            RunTasks();
            RenderBattleAndHUD();
            _allChoices.Render(RenderSize);
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            SetMessageWindowVisibility(true);
            _ = new PartyGUI(_parties[_trainer.Id], PartyGUI.Mode.BattleSwitchIn, OnPartyBrowsingClosed);
        }
        private void CB_FadeFromPartyForBrowsing()
        {
            FadeFromPartyForBrowsing(true);
        }
        private void CB_FadeFromPartyForBrowsing_NoChoices()
        {
            FadeFromPartyForBrowsing(false);
        }
        private void FadeFromPartyForBrowsing(bool showChoices)
        {
            RunTasks();
            RenderBattleAndHUD();
            if (showChoices)
            {
                _allChoices.Render(RenderSize);
            }
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            short result = Game.Instance.Save.Vars[Var.SpecialVar_Result];
            SetMessageWindowVisibility(false);
            if (result == PartyGUI.NO_PKMN_CHOSEN)
            {
                Game.Instance.SetCallback(CB_HandleAllChoices); // No selection, display actions
            }
            else
            {
                ActionsBuilder.PushSwitch(_parties[_trainer.Id].PBEParty[result]);
            }
        }

        private void OnPartyBrowsingClosed()
        {
            Display.SetMinimumWindowSize(RenderSize);
            DayTint.CatchUpTime = true;
            _transition = FadeFromColorTransition.FromBlackStandard();
            short result = Game.Instance.Save.Vars[Var.SpecialVar_Result];
            if (result == PartyGUI.NO_PKMN_CHOSEN)
            {
                Game.Instance.SetCallback(CB_FadeFromPartyForBrowsing); // No selection, display actions
            }
            else
            {
                ClearMessage();
                Game.Instance.SetCallback(CB_FadeFromPartyForBrowsing_NoChoices);
            }
        }

        private void ActionsMenu_PartyChoice()
        {
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeToPartyForBrowsing);
        }

        #endregion

        #region Bag Choice

        private void ActionsMenu_BagChoice()
        {
            // Temporarily auto select
            ActionsBuilder.PushItem(PBEItem.DuskBall);
        }

        #endregion

        #region Run Choice

        private void ActionsMenu_RunChoice()
        {
            CreateCameraMotionTask(DefaultCamPosition, CAM_SPEED_FAST);
            RemoveActionsGUI();
            Game.Instance.SetCallback(CB_RunTasksAndEvents);
            CreateBattleThread(() => _trainer.SelectFleeIfValid(out _));
        }

        #endregion

        #region Back Choice

        private void ActionsMenu_BackChoice()
        {
            ActionsBuilder.Pop();
        }

        #endregion
    }
}
