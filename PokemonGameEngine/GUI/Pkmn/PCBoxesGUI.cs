using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Numerics;

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
        private MainCallbackDelegate _stringReadCallback;

        private bool _isOnParty = false;
        private bool _partyVisible = false;
        private readonly PartyPkmnGUIChoices _partyChoices;
        private readonly GUIString _helpText;
        private GUIString _selectedBoxText;

        private int _selectedBox;
        private int _selectedRow;
        private int _selectedCol;
        private Image[] _selectedBoxMinis;
        private AnimatedImage _selectedMainImage;

        #region Open & Close GUI

        public PCBoxesGUI(PCBoxes boxes, Party party, Action onClosed)
        {
            _boxes = boxes;
            _party = party;

            _partyChoices = new PartyPkmnGUIChoices(0.03f, 0.18f, 0.47f, 0.97f, 0.004f);
            LoadPartyChoices();
            LoadBoxContents();

            _helpText = new GUIString("Press L or R to swap boxes\nPress SELECT to toggle the party\n  choices on or off\nPress START to swap between\n  party and boxes",
                Font.Default, FontColors.DefaultDarkGray_I);

            _onClosed = onClosed;

            _fadeTransition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInPC);
        }

        private void SetExitFadeOutCallback()
        {
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutPC);
        }

        private void CB_FadeInPC()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            Game.Instance.SetCallback(CB_HandleInputs);
        }
        private void CB_FadeOutPC()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            DisposePartyChoices();
            DeleteMinis();
            _helpText.Delete();
            _selectedBoxText.Delete();
            _selectedMainImage?.DeductReference();
            _onClosed();
            _onClosed = null;
        }

        private void RenderFading()
        {
            Render();
            _fadeTransition.Render();
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
            OverwriteStaticString(string.Format("{0} was stored in \"Box {1}\".", pkmn.Nickname, storedIn + 1), CB_ReadOutMessageThenCloseWindow, CB_HandleInputs);
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
            OverwriteStaticString(string.Format("{0} was taken from \"Box {1}\".", pkmn.Nickname, _selectedBox + 1), CB_ReadOutMessageThenCloseWindow, CB_HandleInputs);
        }

        private void BringUpPartyPkmnActions(PartyPokemon pkmn)
        {
            _textChoices = new TextGUIChoices(0, 0, backCommand: CloseChoicesAndStringPrinterThenGoToHandleInputs, font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            _textChoices.AddOne("Deposit", () => Action_DepositPartyPkmn(pkmn));
            _textChoices.AddOne("Cancel", CloseChoicesAndStringPrinterThenGoToHandleInputs);
            CreateDoWhatWithChoices(pkmn.Nickname);
        }
        private void BringUpBoxPkmnActions(BoxPokemon pkmn)
        {
            _textChoices = new TextGUIChoices(0, 0, backCommand: CloseChoicesAndStringPrinterThenGoToHandleInputs, font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            _textChoices.AddOne("Withdraw", () => Action_WithdrawBoxPkmn(pkmn));
            _textChoices.AddOne("Cancel", CloseChoicesAndStringPrinterThenGoToHandleInputs);
            CreateDoWhatWithChoices(pkmn.Nickname);
        }
        private void CreateDoWhatWithChoices(string nickname)
        {
            Size2D s = _textChoices.GetSize();
            _textChoicesWindow = new Window(new RelPos2D(0.6f, 0.3f), s, Colors.White4);
            RenderChoicesOntoWindow();
            string msg = string.Format("Do what with {0}?", nickname);
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
        private void CloseChoicesAndStringPrinterThenGoToHandleInputs()
        {
            CloseChoices();
            CloseStringPrinterAndWindow();
            Game.Instance.SetCallback(CB_HandleInputs);
        }
        private void RenderChoicesOntoWindow()
        {
            _textChoices.RenderChoicesOntoWindow(_textChoicesWindow);
        }

        private void CreateStringPrinterAndWindow(string message, bool isStaticMsg, MainCallbackDelegate doneCallback)
        {
            _stringWindow = Window.CreateStandardMessageBox(Colors.FromRGBA(49, 49, 49, 192));
            _stringPrinter = StringPrinter.CreateStandardMessageBox(_stringWindow, message, Font.Default, FontColors.DefaultWhite_I);
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
        private void OverwriteStaticString(string message, MainCallbackDelegate curCallback, MainCallbackDelegate doneCallback)
        {
            _stringPrinter.Delete();
            _stringPrinter = StringPrinter.CreateStandardMessageBox(_stringWindow, message, Font.Default, FontColors.DefaultWhite_I);
            _stringReadCallback = doneCallback;
            Game.Instance.SetCallback(curCallback);
        }
        private void CloseStringPrinterAndWindow()
        {
            _stringPrinter.Delete();
            _stringPrinter = null;
            _stringWindow.Close();
            _stringWindow = null;
        }
        private void UpdateSelectedBoxText()
        {
            _selectedBoxText?.Delete();
            _selectedBoxText = new GUIString($"BOX {_selectedBox + 1}", Font.Default, FontColors.DefaultDarkGray_I, scale: 2);
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
        private void DeleteMinis()
        {
            if (_selectedBoxMinis is null)
            {
                return;
            }
            for (int i = 0; i < PkmnConstants.BoxCapacity; i++)
            {
                _selectedBoxMinis[i]?.DeductReference();
            }
        }
        private void LoadBoxContents()
        {
            UpdateSelectedBoxText();
            DeleteMinis();
            _selectedBoxMinis = new Image[PkmnConstants.BoxCapacity];
            for (int i = 0; i < PkmnConstants.BoxCapacity; i++)
            {
                BoxPokemon pkmn = _boxes[_selectedBox][i];
                if (pkmn is null)
                {
                    continue;
                }
                _selectedBoxMinis[i] = PokemonImageLoader.GetMini(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny, pkmn.IsEgg);
            }
            LoadPkmnContents(GetSelectedBoxPkmn());
        }
        private void LoadPkmnContents(BoxPokemon pkmn)
        {
            _selectedMainImage?.DeductReference();
            if (pkmn is null)
            {
                _selectedMainImage = null;
                return;
            }
            _selectedMainImage = PokemonImageLoader.GetPokemonImage(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny, false, false, pkmn.PID, pkmn.IsEgg);
        }

        private void CB_Choices()
        {
            int s = _textChoices.Selected;
            _textChoices.HandleInputs();
            // Check if the window was just closed
            if (_textChoicesWindow is not null && s != _textChoices.Selected)
            {
                RenderChoicesOntoWindow(); // Update selection if it has changed
            }

            Render();
        }
        private void CB_ReadOutStaticMessage()
        {
            _stringPrinter.Update();
            if (_stringPrinter.IsEnded)
            {
                Game.Instance.SetCallback(_stringReadCallback);
                _stringReadCallback = null;
            }

            Render();
        }
        private void CB_ReadOutMessageThenCloseWindow()
        {
            _stringPrinter.Update();
            if (_stringPrinter.IsDone)
            {
                CloseStringPrinterAndWindow();
                Game.Instance.SetCallback(_stringReadCallback);
                _stringReadCallback = null;
            }

            Render();
        }
        private void CB_ReadOutMessageThenRestoreStaticBackup()
        {
            _stringPrinter.Update();
            if (_stringPrinter.IsDone)
            {
                OverwriteStaticString(_staticStringBackup, CB_ReadOutStaticMessage, _stringReadCallback);
                _staticStringBackup = null;
                // Don't set _stringReadCallback back to null because it's needed
            }

            Render();
        }
        private void CB_HandleInputs()
        {
            HandleInputs();
            Render();
        }

        private void HandleInputs()
        {
            if (InputManager.IsPressed(Key.B))
            {
                SetExitFadeOutCallback();
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

        private void Render()
        {
            GL gl = Display.OpenGL;
            // Background
            //Renderer.ThreeColorBackground(dst, dstW, dstH, Renderer.Color(215, 231, 230, 255), Renderer.Color(231, 163, 0, 255), Renderer.Color(242, 182, 32, 255));
            GLHelper.ClearColor(gl, Colors.FromRGB(31, 31, 31));
            gl.Clear(ClearBufferMask.ColorBufferBit);

            // PC
            _selectedBoxText.Render(Pos2D.FromRelative(0.02f, 0.01f));

            if (_partyVisible)
            {
                _partyChoices.Render();
            }
            else
            {
                if (_selectedMainImage is not null)
                {
                    _selectedMainImage.Update();
                    _selectedMainImage.Render(Pos2D.CenterXBottomY(0.24f, 0.6f, _selectedMainImage.Size));
                }
                _helpText.Render(Pos2D.FromRelative(0.015f, 0.62f));
            }

            // Draw boxes
            int boxStartX = Renderer.RelXToAbsX(0.48f);
            int boxStartY = Renderer.RelYToAbsY(0.05f);
            for (int i = 0; i < PkmnConstants.BoxCapacity; i++)
            {
                int x = i % NumPerRow;
                int y = i / NumPerRow;
                int px = boxStartX + (x * 40);
                int py = boxStartY + (y * 40);
                Vector4 color = _selectedCol == y && _selectedRow == x ? Colors.FromRGBA(0, 0, 0, 32) : Colors.FromRGBA(0, 0, 0, 64);
                GUIRenderer.Instance.FillRectangle(color, new Rect2D(new Pos2D(px, py), new Size2D(38, 38)));

                Image mini = _selectedBoxMinis[i];
                if (mini is null)
                {
                    continue;
                }
                mini.Render(new Pos2D(px + 3, py + 3));
            }

            // Dim the side we're not using
            if (_isOnParty)
            {
                GUIRenderer.Instance.FillRectangle(Colors.FromRGBA(0, 0, 0, 128), new Rect2D(Pos2D.FromRelative(0.48f, 0f), Size2D.FromRelative(0.52f, 1f)));
            }
            else if (_partyVisible)
            {
                GUIRenderer.Instance.FillRectangle(Colors.FromRGBA(0, 0, 0, 128), new Rect2D(new Pos2D(0, 0), Size2D.FromRelative(0.48f, 1f)));
            }

            Window.RenderAll();
        }
    }
}
