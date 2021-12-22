﻿using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Pkmn;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Render.World;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed partial class BattleGUI
    {
        public SwitchesBuilder SwitchesBuilder;

        public void SubmitSwitches(PBESwitchIn[] switches)
        {
            SwitchesBuilder = null;
            Game.Instance.SetCallback(CB_RunTasksAndEvents);
            CreateBattleThread(() => _trainer.SelectSwitchesIfValid(out _, switches));
        }

        public bool CanUsePositionForBattleReplacement(PBEFieldPosition pos)
        {
            return !SwitchesBuilder.IsStandBy(pos) && _trainer.OwnsSpot(pos) && !_trainer.Team.IsSpotOccupied(pos);
        }

        private void InitFadeToPartyForReplacement()
        {
            // TODO: Run from wild?
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeToPartyForReplacement);
        }
        private void CB_FadeToPartyForReplacement()
        {
            _tasks.RunTasks();
            RenderBattle();
            _transition.Render();
            _frameBuffer.RenderToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            SetMessageWindowVisibility(true);
            _ = new PartyGUI(_parties[_trainer.Id], PartyGUI.Mode.BattleReplace, OnPartyReplacementClosed);
        }

        private void OnPartyReplacementClosed()
        {
            _frameBuffer.Use();
            DayTint.CatchUpTime = true;
            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeFromPartyReplacement);
        }
        private void CB_FadeFromPartyReplacement()
        {
            _tasks.RunTasks();
            RenderBattle();
            _transition.Render();
            _frameBuffer.RenderToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            SetMessageWindowVisibility(false);
            SwitchesBuilder.Submit(); // Calls SubmitSwitches()
        }
    }
}
