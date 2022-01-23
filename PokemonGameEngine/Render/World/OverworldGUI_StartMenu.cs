using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Pkmn;
using Kermalis.PokemonGameEngine.Render.Player;
using Kermalis.PokemonGameEngine.Render.Transitions;
#if DEBUG
using System;
#endif

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed partial class OverworldGUI
    {
        private const int START_MENU_X = 192;
        private const int START_MENU_Y = 8;

        private Window _startMenuWindow;
        private TextGUIChoices _startMenuChoices;

        public void OpenStartMenu()
        {
            SetupStartMenuWindow();
            Game.Instance.SetCallback(CB_StartMenu);
        }

        private void StartMenu_BagSelected()
        {
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutToBag);
        }
#if DEBUG
        private void Debug_StartMenu_PCSelected()
        {
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutToPC);
        }
        private void Debug_StartMenu_QuitSelected()
        {
            Engine.RequestQuit();
        }
        private void Debug_StartMenu_OpenSubMenu(Action setupChoices)
        {
            _startMenuChoices.Dispose();
            _startMenuWindow.Close();
            setupChoices();
            SetupStartMenuWindow();
        }
#endif
        private void SetupStartMenuChoices()
        {
            _startMenuChoices = new TextGUIChoices(0f, 0f, backCommand: CloseStartMenuAndSetCB,
                font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            _startMenuChoices.AddOne("Pokémon", () => OpenPartyMenu(PartyGUI.Mode.PkmnMenu));
            _startMenuChoices.AddOne("Bag", StartMenu_BagSelected);
            _startMenuChoices.AddOne("Close", CloseStartMenuAndSetCB);
#if DEBUG
            _startMenuChoices.AddOne("Debug Menu", Debug_SetDebugMenu);
#endif
        }
#if DEBUG
        private void Debug_SetStartMenu()
        {
            Debug_StartMenu_OpenSubMenu(SetupStartMenuChoices);
        }
        private void Debug_SetDebugMenu()
        {
            Debug_StartMenu_OpenSubMenu(Debug_SetupDebugStartMenuChoices);
        }
        private void Debug_SetupDebugStartMenuChoices()
        {
            _startMenuChoices = new TextGUIChoices(0f, 0f, backCommand: Debug_SetStartMenu,
                font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            _startMenuChoices.AddOne("PC", Debug_StartMenu_PCSelected);
#if DEBUG_OVERWORLD
            _startMenuChoices.AddOne("MapRenderer Menu", Debug_SetDebugMapRendererMenu);
#endif
            _startMenuChoices.AddOne("Back", Debug_SetStartMenu);
            _startMenuChoices.AddOne("Quit", Debug_StartMenu_QuitSelected);
        }
#endif
#if DEBUG_OVERWORLD
        private void Debug_SetDebugMapRendererMenu()
        {
            Debug_StartMenu_OpenSubMenu(Debug_SetupDebugMapRendererStartMenuChoices);
        }
        private void Debug_SetupDebugMapRendererStartMenuChoices()
        {
            _startMenuChoices = new TextGUIChoices(0f, 0f, backCommand: Debug_SetDebugMenu,
                font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            _startMenuChoices.AddOne("Toggle (R)", _mapRenderer.Debug_Toggle);
            _startMenuChoices.AddOne("Toggle Grid", _mapRenderer.Debug_ToggleBlockGrid);
            _startMenuChoices.AddOne("Toggle Statuses", _mapRenderer.Debug_ToggleBlockStatus);
            _startMenuChoices.AddOne("Toggle Texts", _mapRenderer.Debug_ToggleBlockText);
            _startMenuChoices.AddOne("Back", Debug_SetDebugMenu);
        }
#endif

        private void SetupStartMenuWindow()
        {
            _startMenuWindow = Window.CreateFromInnerSize(new Vec2I(START_MENU_X, START_MENU_Y), _startMenuChoices.GetSize(), Colors.White4, Window.Decoration.GrayRounded);
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
            Display.SetMinimumWindowSize(RenderSize);
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
            _ = new BagGUI(Game.Instance.Save.PlayerInventory, ReturnToStartMenuWithFadeIn);
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

            TextGUIChoices prevMenu = _startMenuChoices;
            int prevSelection = prevMenu.Selected;
            _startMenuChoices.HandleInputs();
            // Check if the window was just closed
            if (_startMenuWindow is not null && (prevMenu != _startMenuChoices || prevSelection != _startMenuChoices.Selected))
            {
                RenderStartMenuChoicesOntoWindow(); // Update selection if it has changed or if the menu is different
            }
        }
    }
}
