using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Pkmn;
using Kermalis.PokemonGameEngine.Render.Player;
using Kermalis.PokemonGameEngine.Render.Transitions;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed partial class OverworldGUI
    {
        private Window _startMenuWindow;
        private TextGUIChoices _startMenuChoices;

        public void OpenStartMenu()
        {
            SetupStartMenuWindow();
            Game.Instance.SetCallback(CB_StartMenu);
        }

        private void StartMenu_DebugBagSelected()
        {
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutToBag);
        }
        private void StartMenu_DebugPCSelected()
        {
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutToPC);
        }
        private void StartMenu_DebugQuitSelected()
        {
            Engine.RequestQuit();
        }
        private void SetupStartMenuChoices()
        {
            _startMenuChoices = new TextGUIChoices(0f, 0f, backCommand: CloseStartMenuAndSetCB,
                font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            _startMenuChoices.AddOne("Pokémon", () => OpenPartyMenu(PartyGUI.Mode.PkmnMenu));
            _startMenuChoices.AddOne("Bag", StartMenu_DebugBagSelected);
            _startMenuChoices.AddOne("PC", StartMenu_DebugPCSelected);
            _startMenuChoices.AddOne("Close", CloseStartMenuAndSetCB);
            _startMenuChoices.AddOne("Quit", StartMenu_DebugQuitSelected);
        }

        private void SetupStartMenuWindow()
        {
            Vec2I s = _startMenuChoices.GetSize();
            _startMenuWindow = new Window(Vec2I.FromRelative(0.72f, 0.05f, RenderSize), s, Colors.White4);
            RenderStartMenuChoicesOntoWindow();
        }
        private void RenderStartMenuChoicesOntoWindow()
        {
            _startMenuChoices.RenderChoicesOntoWindow(_startMenuWindow);
        }
        private void CloseStartMenuAndSetCB()
        {
            _startMenuWindow.Close();
            _startMenuWindow = null;
            Game.Instance.SetCallback(CB_ProcessScriptsTasksAndObjs);
        }

        private void ReturnToStartMenuWithFadeIn()
        {
            DayTint.CatchUpTime = true;
            SetupStartMenuWindow();

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInToStartMenu);
        }

        private void CB_FadeInToStartMenu()
        {
            Render();
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            Game.Instance.IsOnOverworld = true;
            Game.Instance.SetCallback(CB_StartMenu);
        }
        private void CB_FadeOutToParty_PkmnMenu()
        {
            Render();
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            _startMenuWindow.Close();
            _startMenuWindow = null;
            _ = new PartyGUI(Game.Instance.Save.PlayerParty, PartyGUI.Mode.PkmnMenu, ReturnToStartMenuWithFadeIn);
        }
        private void CB_FadeOutToParty_SelectDaycare()
        {
            Render();
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            _ = new PartyGUI(Game.Instance.Save.PlayerParty, PartyGUI.Mode.SelectDaycare, ReturnToFieldWithFadeIn);
        }
        private void CB_FadeOutToBag()
        {
            Render();
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            _startMenuWindow.Close();
            _startMenuWindow = null;
            _ = new BagGUI(Game.Instance.Save.PlayerInventory, Game.Instance.Save.PlayerParty, ReturnToStartMenuWithFadeIn);
        }
        private void CB_FadeOutToPC()
        {
            Render();
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            _startMenuWindow.Close();
            _startMenuWindow = null;
            _ = new PCBoxesGUI(Game.Instance.Save.PCBoxes, Game.Instance.Save.PlayerParty, ReturnToStartMenuWithFadeIn);
        }
        private void CB_StartMenu()
        {
            Render();
            _frameBuffer.BlitToScreen();

            int s = _startMenuChoices.Selected;
            _startMenuChoices.HandleInputs();
            // Check if the window was just closed
            if (_startMenuWindow is not null && s != _startMenuChoices.Selected)
            {
                RenderStartMenuChoicesOntoWindow(); // Update selection if it has changed
            }
        }
    }
}
