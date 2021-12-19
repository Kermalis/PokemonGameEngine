using Kermalis.PokemonBattleEngine.Battle;
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
            Game.Instance.SetCallback(CB_RunTasksAndEvents);
            CreateBattleThread(() => Trainer.SelectSwitchesIfValid(out _, switches));
        }

        private void InitFadeToPartyForReplacement()
        {
            // TODO: Run from wild?
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeToPartyForReplacement);
        }
        private void CB_FadeToPartyForReplacement()
        {
            _tasks.RunTasks();
            RenderBattle();
            _fadeTransition.Render();
            _frameBuffer.RenderToScreen();

            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            SetMessageWindowVisibility(true);
            _ = new PartyGUI(_parties[Trainer.Id], PartyGUI.Mode.BattleReplace, OnPartyReplacementClosed);
        }

        private void OnPartyReplacementClosed()
        {
            _frameBuffer.Use();
            DayTint.CatchUpTime = true;
            _fadeTransition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeFromPartyReplacement);
        }
        private void CB_FadeFromPartyReplacement()
        {
            _tasks.RunTasks();
            RenderBattle();
            _fadeTransition.Render();
            _frameBuffer.RenderToScreen();

            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            SetMessageWindowVisibility(false);
            SwitchesBuilder.Submit(); // Calls SubmitSwitches()
        }
    }
}
