using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    internal sealed class PCBoxesGUI
    {
        private const int NumPerRow = 5;
        private const int NumColumns = PkmnConstants.BoxCapacity / NumPerRow; // Won't work if it's not evenly divisible

        private readonly PCBoxes _boxes;
        private readonly Party _party;

        private FadeColorTransition _fadeTransition;
        private Action _onClosed;

        private Window _textChoicesWindow;
        private TextGUIChoices _textChoices;
        private Window _stringWindow;
        private StringPrinter _stringPrinter;
        private string _staticStringBackup;
        private MainCallback _stringReadCallback;

        private bool _isOnParty = false;
        private bool _partyVisible = false;
        private readonly PartyPkmnGUIChoices _partyChoices;

        private int _selectedBox;
        private int _selectedRow;
        private int _selectedCol;
        private Image[] _selectedBoxMinis;
        private AnimatedImage _selectedMainImage;

        #region Open & Close GUI

        public unsafe PCBoxesGUI(PCBoxes boxes, Party party, Action onClosed)
        {
            _boxes = boxes;
            _party = party;

            _partyChoices = new PartyPkmnGUIChoices(0.03f, 0.18f, 0.47f, 0.97f, 0.004f);
            LoadPartyChoices();
            LoadBoxContents();

            _onClosed = onClosed;
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeInPC);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void ClosePCMenu()
        {
            _fadeTransition = new FadeToColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeOutPC);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void CB_FadeInPC()
        {
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Game.Instance.SetCallback(CB_LogicTick);
                Game.Instance.SetRCallback(RCB_RenderTick);
            }
        }
        private unsafe void CB_FadeOutPC()
        {
            if (_fadeTransition.IsDone)
            {
                DisposePartyChoices();
                _fadeTransition = null;
                _onClosed.Invoke();
                _onClosed = null;
            }
        }

        private unsafe void RCB_Fading(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RCB_RenderTick(bmpAddress, bmpWidth, bmpHeight);
            _fadeTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
        }

        #endregion

        private void Action_DepositPartyPkmn(PartyPokemon pkmn)
        {
            if (_party.Count == 1)
            {
                OverwriteStaticString("That's your last Pokémon!", CB_ReadOutMessageThenRestoreStaticBackup, CB_Choices);
                return;
            }
            int storedIn = _boxes.Add(pkmn);
            if (storedIn == -1)
            {
                OverwriteStaticString("There's no space left in the PC!", CB_ReadOutMessageThenRestoreStaticBackup, CB_Choices);
                return;
            }
            // Success
            _party.Remove(pkmn);
            DisposePartyChoices();
            LoadPartyChoices();
            if (_selectedBox == storedIn)
            {
                LoadBoxContents();
            }
            CloseChoices();
            OverwriteStaticString(string.Format("{0} was stored in \"Box {1}\".", pkmn.Nickname, storedIn + 1), CB_ReadOutMessageThenCloseWindow, CB_LogicTick);
        }
        private void Action_WithdrawBoxPkmn(BoxPokemon pkmn)
        {
            if (_party.Count == PkmnConstants.PartyCapacity)
            {
                OverwriteStaticString("Your party is full!", CB_ReadOutMessageThenRestoreStaticBackup, CB_Choices);
                return;
            }
            // Success
            _party.Add(new PartyPokemon(pkmn));
            _boxes[_selectedBox].Remove(pkmn);
            DisposePartyChoices();
            LoadPartyChoices();
            LoadBoxContents();
            CloseChoices();
            OverwriteStaticString(string.Format("{0} was taken from \"Box {1}\".", pkmn.Nickname, _selectedBox + 1), CB_ReadOutMessageThenCloseWindow, CB_LogicTick);
        }

        private void BringUpPartyPkmnActions(PartyPokemon pkmn)
        {
            _textChoices = new TextGUIChoices(0, 0, backCommand: CloseChoicesAndStringPrinterThenGoToLogicTick, font: Font.Default, fontColors: Font.DefaultDarkGray_I, selectedColors: Font.DefaultYellow_O);
            _textChoices.Add(new TextGUIChoice("Deposit", () => Action_DepositPartyPkmn(pkmn)));
            _textChoices.Add(new TextGUIChoice("Cancel", CloseChoicesAndStringPrinterThenGoToLogicTick));
            _textChoices.GetSize(out int width, out int height);
            _textChoicesWindow = new Window(0.6f, 0.3f, width, height, RenderUtils.Color(255, 255, 255, 255));
            RenderChoicesOntoWindow();
            string msg = string.Format("Do what with {0}?", pkmn.Nickname);
            _staticStringBackup = msg;
            CreateStringPrinterAndWindow(msg, true, CB_Choices);
        }
        private void BringUpBoxPkmnActions(BoxPokemon pkmn)
        {
            _textChoices = new TextGUIChoices(0, 0, backCommand: CloseChoicesAndStringPrinterThenGoToLogicTick, font: Font.Default, fontColors: Font.DefaultDarkGray_I, selectedColors: Font.DefaultYellow_O);
            _textChoices.Add(new TextGUIChoice("Withdraw", () => Action_WithdrawBoxPkmn(pkmn)));
            _textChoices.Add(new TextGUIChoice("Cancel", CloseChoicesAndStringPrinterThenGoToLogicTick));
            _textChoices.GetSize(out int width, out int height);
            _textChoicesWindow = new Window(0.6f, 0.3f, width, height, RenderUtils.Color(255, 255, 255, 255));
            RenderChoicesOntoWindow();
            string msg = string.Format("Do what with {0}?", pkmn.Nickname);
            _staticStringBackup = msg;
            CreateStringPrinterAndWindow(msg, true, CB_Choices);
        }
        private void CloseChoices()
        {
            _textChoicesWindow.Close();
            _textChoicesWindow = null;
            _textChoices.Dispose();
            _textChoices = null;
        }
        private void CloseChoicesAndStringPrinterThenGoToLogicTick()
        {
            CloseChoices();
            CloseStringPrinterAndWindow();
            Game.Instance.SetCallback(CB_LogicTick);
        }
        private unsafe void RenderChoicesOntoWindow()
        {
            _textChoicesWindow.ClearImage();
            Image i = _textChoicesWindow.Image;
            fixed (uint* bmpAddress = i.Bitmap)
            {
                _textChoices.Render(bmpAddress, i.Width, i.Height);
            }
        }

        private void CreateStringPrinterAndWindow(string message, bool isStaticMsg, MainCallback doneCallback)
        {
            _stringWindow = new Window(0, 0.79f, 1, 0.16f, RenderUtils.Color(49, 49, 49, 192));
            _stringPrinter = new StringPrinter(_stringWindow, message, 0.1f, 0.01f, Font.Default, Font.DefaultWhite1_I);
            _stringReadCallback = doneCallback;
            if (isStaticMsg)
            {
                Game.Instance.SetCallback(CB_ReadOutStaticMessage);
            }
            else
            {
                Game.Instance.SetCallback(CB_ReadOutMessageThenCloseWindow);
            }
        }
        private void OverwriteStaticString(string message, MainCallback curCallback, MainCallback doneCallback)
        {
            _stringPrinter.Close();
            _stringPrinter = new StringPrinter(_stringWindow, message, 0.1f, 0.01f, Font.Default, Font.DefaultWhite1_I);
            _stringReadCallback = doneCallback;
            Game.Instance.SetCallback(curCallback);
        }
        private void CloseStringPrinterAndWindow()
        {
            _stringPrinter.Close();
            _stringPrinter = null;
            _stringWindow.Close();
            _stringWindow = null;
        }

        private BoxPokemon GetSelectedBoxPkmn()
        {
            return _boxes[_selectedBox][_selectedRow + (_selectedCol * NumPerRow)];
        }

        private void LoadPartyChoices()
        {
            foreach (PartyPokemon pkmn in _party)
            {
                _partyChoices.Add(new PartyPkmnGUIChoice(pkmn, () => BringUpPartyPkmnActions(pkmn)));
            }
        }
        private void DisposePartyChoices()
        {
            _partyChoices.Dispose();
            _partyChoices.Clear();
        }
        private void LoadBoxContents()
        {
            _selectedBoxMinis = new Image[PkmnConstants.BoxCapacity];
            for (int i = 0; i < PkmnConstants.BoxCapacity; i++)
            {
                BoxPokemon pkmn = _boxes[_selectedBox][i];
                if (pkmn is null)
                {
                    continue;
                }
                _selectedBoxMinis[i] = PokemonImageUtils.GetMini(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny, pkmn.IsEgg);
            }
            LoadPkmnContents(GetSelectedBoxPkmn());
        }
        private void LoadPkmnContents(BoxPokemon pkmn)
        {
            if (pkmn is null)
            {
                _selectedMainImage = null;
                return;
            }
            _selectedMainImage = PokemonImageUtils.GetPokemonImage(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny, false, false, pkmn.PID, pkmn.IsEgg);
        }

        private void CB_Choices()
        {
            int s = _textChoices.Selected;
            _textChoices.HandleInputs();
            if (_textChoicesWindow is null)
            {
                return; // Was just closed
            }
            if (s != _textChoices.Selected)
            {
                RenderChoicesOntoWindow();
            }
        }
        private void CB_ReadOutStaticMessage()
        {
            _stringPrinter.LogicTick();
            if (_stringPrinter.IsEnded)
            {
                Game.Instance.SetCallback(_stringReadCallback);
                _stringReadCallback = null;
            }
        }
        private void CB_ReadOutMessageThenCloseWindow()
        {
            _stringPrinter.LogicTick();
            if (_stringPrinter.IsDone)
            {
                CloseStringPrinterAndWindow();
                Game.Instance.SetCallback(_stringReadCallback);
                _stringReadCallback = null;
            }
        }
        private void CB_ReadOutMessageThenRestoreStaticBackup()
        {
            _stringPrinter.LogicTick();
            if (_stringPrinter.IsDone)
            {
                OverwriteStaticString(_staticStringBackup, CB_ReadOutStaticMessage, _stringReadCallback);
                _staticStringBackup = null;
                // Don't set _stringReadCallback back to null because it's needed
            }
        }
        private void CB_LogicTick()
        {
            if (InputManager.IsPressed(Key.B))
            {
                ClosePCMenu();
                return;
            }
            if (_partyVisible && _isOnParty)
            {
                if (InputManager.IsPressed(Key.Start))
                {
                    _isOnParty = false;
                    return;
                }
                _partyChoices.HandleInputs();
            }
            else
            {
                if (_partyVisible && InputManager.IsPressed(Key.Start))
                {
                    _isOnParty = true;
                    return;
                }
                HandlePCInputs();
            }
        }

        private void HandlePCInputs()
        {
            if (InputManager.IsPressed(Key.A))
            {
                BringUpBoxPkmnActions(GetSelectedBoxPkmn());
            }
            if (InputManager.IsPressed(Key.R))
            {
                if (++_selectedBox >= PkmnConstants.NumBoxes)
                {
                    _selectedBox = 0;
                }
                LoadBoxContents();
                return;
            }
            if (InputManager.IsPressed(Key.L))
            {
                if (--_selectedBox < 0)
                {
                    _selectedBox = PkmnConstants.NumBoxes - 1;
                }
                LoadBoxContents();
                return;
            }
            if (InputManager.IsPressed(Key.Select))
            {
                _partyVisible = !_partyVisible;
                return;
            }
            if (InputManager.IsPressed(Key.Right))
            {
                if (++_selectedRow >= NumPerRow)
                {
                    _selectedRow = 0;
                }
                LoadPkmnContents(GetSelectedBoxPkmn());
                return;
            }
            if (InputManager.IsPressed(Key.Left))
            {
                if (--_selectedRow < 0)
                {
                    _selectedRow = NumPerRow - 1;
                }
                LoadPkmnContents(GetSelectedBoxPkmn());
                return;
            }
            if (InputManager.IsPressed(Key.Down))
            {
                if (++_selectedCol >= NumColumns)
                {
                    _selectedCol = 0;
                }
                LoadPkmnContents(GetSelectedBoxPkmn());
                return;
            }
            if (InputManager.IsPressed(Key.Up))
            {
                if (--_selectedCol < 0)
                {
                    _selectedCol = NumColumns - 1;
                }
                LoadPkmnContents(GetSelectedBoxPkmn());
                return;
            }
        }

        private unsafe void RCB_RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            // Background
            RenderUtils.ThreeColorBackground(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(215, 231, 230, 255), RenderUtils.Color(231, 163, 0, 255), RenderUtils.Color(242, 182, 32, 255));

            // PC
            Font.Default.DrawStringScaled(bmpAddress, bmpWidth, bmpHeight, 0.02f, 0.01f, 2, $"BOX {_selectedBox + 1}", Font.DefaultDarkGray_I);

            if (_partyVisible)
            {
                _partyChoices.Render(bmpAddress, bmpWidth, bmpHeight);
            }
            else
            {
                if (_selectedMainImage != null)
                {
                    AnimatedImage.UpdateCurrentFrameForAll();
                    _selectedMainImage.DrawOn(bmpAddress, bmpWidth, bmpHeight,
                        RenderUtils.GetCoordinatesForCentering(bmpWidth, _selectedMainImage.Width, 0.24f), RenderUtils.GetCoordinatesForEndAlign(bmpHeight, _selectedMainImage.Height, 0.6f));
                }
                Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, 0.015f, 0.62f,
                    "Press L or R to swap boxes\nPress SELECT to toggle the party\n  choices on or off\nPress START to swap between\n  party and boxes",
                    Font.DefaultDarkGray_I);
            }

            // Draw boxes
            int boxStartX = (int)(bmpWidth * 0.48f);
            int boxStartY = (int)(bmpHeight * 0.05f);
            for (int i = 0; i < PkmnConstants.BoxCapacity; i++)
            {
                int x = i % NumPerRow;
                int y = i / NumPerRow;
                int px = boxStartX + (x * 40);
                int py = boxStartY + (y * 40);
                uint color = _selectedCol == y && _selectedRow == x ? RenderUtils.Color(0, 0, 0, 32) : RenderUtils.Color(0, 0, 0, 64);
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, px, py, 38, 38, color);

                Image mini = _selectedBoxMinis[i];
                if (mini is null)
                {
                    continue;
                }
                mini.DrawOn(bmpAddress, bmpWidth, bmpHeight, px + 3, py + 3);
            }

            // Dim the side we're not using
            if (_isOnParty)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.48f, 0, 0.52f, 1, RenderUtils.Color(0, 0, 0, 128));
            }
            else if (_partyVisible)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0, 0, 0.48f, 1, RenderUtils.Color(0, 0, 0, 128));
            }

            Game.Instance.RenderWindows(bmpAddress, bmpWidth, bmpHeight);
        }
    }
}
