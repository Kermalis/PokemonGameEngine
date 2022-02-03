using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Transitions;
using Silk.NET.OpenGL;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Pkmn
{
    internal sealed class PCBoxesGUI
    {
        private static readonly Vec2I _renderSize = new(480, 270); // 16:9
        private readonly FrameBuffer _frameBuffer;
        private readonly TripleColorBackground _tripleColorBG;

        private const int NumPerRow = 6;
        private const int NumColumns = PkmnConstants.BoxCapacity / NumPerRow; // Won't work if it's not evenly divisible

        private readonly PCBoxes _boxes;
        private readonly Party _party;

        private ITransition _transition;
        private Action _onClosed;

        private Window _textChoicesWindow;
        private TextGUIChoices _textChoices;
        private Window _stringWindow;
        private StringPrinter _stringPrinter;
        private string _staticStringBackup;
        private Action _stringReadCallback;

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
            Display.SetMinimumWindowSize(_renderSize);
            _frameBuffer = new FrameBuffer().AddColorTexture(_renderSize);

            _tripleColorBG = new TripleColorBackground();
            _tripleColorBG.SetColors(Colors.FromRGB(235, 230, 255), Colors.FromRGB(180, 240, 140), Colors.FromRGB(0, 255, 140));

            _boxes = boxes;
            _party = party;

            _partyChoices = new PartyPkmnGUIChoices(new Vector2(0.03f, 0.18f), new Vector2(0.47f, 0.97f), 0.004f);
            LoadPartyChoices();
            LoadBoxContents();

            _helpText = new GUIString("Press L or R to swap boxes\nPress SELECT to toggle the party\n  choices on or off\nPress START to swap between\n  party and boxes",
                Font.Default, FontColors.DefaultDarkGray_I);

            _onClosed = onClosed;

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInPC);
        }

        private void SetExitFadeOutCallback()
        {
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutPC);
        }

        private void CB_FadeInPC()
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
            Game.Instance.SetCallback(CB_HandleInputs);
        }
        private void CB_FadeOutPC()
        {
            Render();
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _frameBuffer.Delete();
            _tripleColorBG.Delete();
            DisposePartyChoices();
            DeleteMinis();
            _helpText.Delete();
            _selectedBoxText.Delete();
            _selectedMainImage?.DeductReference();
            _onClosed();
            _onClosed = null;
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
            _textChoices = new TextGUIChoices(0f, 0f, backCommand: CloseChoicesAndStringPrinterThenGoToHandleInputs,
                font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            _textChoices.AddOne("Deposit", () => Action_DepositPartyPkmn(pkmn));
            _textChoices.AddOne("Cancel", CloseChoicesAndStringPrinterThenGoToHandleInputs);
            CreateDoWhatWithChoices(pkmn.Nickname);
        }
        private void BringUpBoxPkmnActions(BoxPokemon pkmn)
        {
            _textChoices = new TextGUIChoices(0f, 0f, backCommand: CloseChoicesAndStringPrinterThenGoToHandleInputs,
                font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            _textChoices.AddOne("Withdraw", () => Action_WithdrawBoxPkmn(pkmn));
            _textChoices.AddOne("Cancel", CloseChoicesAndStringPrinterThenGoToHandleInputs);
            CreateDoWhatWithChoices(pkmn.Nickname);
        }
        private void CreateDoWhatWithChoices(string nickname)
        {
            _textChoicesWindow = Window.CreateFromInnerSize(Vec2I.FromRelative(0.55f, 0.25f, _renderSize), _textChoices.GetSize(), Colors.White4, Window.Decoration.GrayRounded);
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

        private void CreateStringPrinterAndWindow(string message, bool isStaticMsg, Action doneCallback)
        {
            _stringWindow = Window.CreateStandardMessageBox(Colors.FromRGBA(20, 20, 20, 225), _renderSize);
            _stringPrinter = new StringPrinter(_stringWindow, message, Font.Default, FontColors.DefaultWhite_I, new Vec2I(8, 0));
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
        private void OverwriteStaticString(string message, Action curCallback, Action doneCallback)
        {
            _stringPrinter.Dispose();
            _stringPrinter = new StringPrinter(_stringWindow, message, Font.Default, FontColors.DefaultWhite_I, new Vec2I(8, 0));
            _stringReadCallback = doneCallback;
            Game.Instance.SetCallback(curCallback);
        }
        private void CloseStringPrinterAndWindow()
        {
            _stringPrinter.Dispose();
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
                if (pkmn.IsEgg)
                {
                    _selectedBoxMinis[i] = PokemonImageLoader.GetEggMini();
                }
                else
                {
                    _selectedBoxMinis[i] = PokemonImageLoader.GetMini(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny);
                }
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
            if (pkmn.IsEgg)
            {
                _selectedMainImage = PokemonImageLoader.GetEggImage();
            }
            else
            {
                _selectedMainImage = PokemonImageLoader.GetPokemonImage(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny, pkmn.PID, false);
            }
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
            _frameBuffer.BlitToScreen();
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
            _frameBuffer.BlitToScreen();
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
            _frameBuffer.BlitToScreen();
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
            _frameBuffer.BlitToScreen();
        }
        private void CB_HandleInputs()
        {
            Render();
            _frameBuffer.BlitToScreen();

            HandleInputs();
        }

        private void HandleInputs()
        {
            if (InputManager.JustPressed(Key.B))
            {
                SetExitFadeOutCallback();
                return;
            }

            if (_partyVisible && _isOnParty)
            {
                if (InputManager.JustPressed(Key.Start))
                {
                    _isOnParty = false;
                    return;
                }
                _partyChoices.HandleInputs();
            }
            else
            {
                if (_partyVisible && InputManager.JustPressed(Key.Start))
                {
                    _isOnParty = true;
                    return;
                }
                HandlePCInputs();
            }
        }
        private void HandlePCInputs()
        {
            if (InputManager.JustPressed(Key.A))
            {
                BringUpBoxPkmnActions(GetSelectedBoxPkmn());
            }
            if (InputManager.JustPressed(Key.R))
            {
                if (++_selectedBox >= PkmnConstants.NumBoxes)
                {
                    _selectedBox = 0;
                }
                LoadBoxContents();
                return;
            }
            if (InputManager.JustPressed(Key.L))
            {
                if (--_selectedBox < 0)
                {
                    _selectedBox = PkmnConstants.NumBoxes - 1;
                }
                LoadBoxContents();
                return;
            }
            if (InputManager.JustPressed(Key.Select))
            {
                _partyVisible = !_partyVisible;
                return;
            }
            if (InputManager.JustPressed(Key.Right))
            {
                if (++_selectedRow >= NumPerRow)
                {
                    _selectedRow = 0;
                }
                LoadPkmnContents(GetSelectedBoxPkmn());
                return;
            }
            if (InputManager.JustPressed(Key.Left))
            {
                if (--_selectedRow < 0)
                {
                    _selectedRow = NumPerRow - 1;
                }
                LoadPkmnContents(GetSelectedBoxPkmn());
                return;
            }
            if (InputManager.JustPressed(Key.Down))
            {
                if (++_selectedCol >= NumColumns)
                {
                    _selectedCol = 0;
                }
                LoadPkmnContents(GetSelectedBoxPkmn());
                return;
            }
            if (InputManager.JustPressed(Key.Up))
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
            _frameBuffer.UseAndViewport(gl);
            _tripleColorBG.Render(gl); // No need to glClear since this overwrites everything

            // PC
            _selectedBoxText.Render(Vec2I.FromRelative(0.02f, 0.01f, _renderSize));

            if (_partyVisible)
            {
                _partyChoices.Render(_renderSize);
                _frameBuffer.UseAndViewport(gl); // Possible the above redraws to its framebuffer so rebind this one
            }
            else
            {
                if (_selectedMainImage is not null)
                {
                    _selectedMainImage.Update();
                    _selectedMainImage.Render(Vec2I.CenterXBottomY(0.24f, 0.6f, _selectedMainImage.Size, _renderSize));
                }
                _helpText.Render(Vec2I.FromRelative(0.015f, 0.62f, _renderSize));
            }

            // Draw boxes
            int boxStartX = (int)(0.48f * _renderSize.X);
            int boxStartY = (int)(0.05f * _renderSize.Y);
            for (int i = 0; i < PkmnConstants.BoxCapacity; i++)
            {
                int x = i % NumPerRow;
                int y = i / NumPerRow;
                Vec2I pos;
                pos.X = boxStartX + (x * 40);
                pos.Y = boxStartY + (y * 40);
                Vector4 color = _selectedCol == y && _selectedRow == x ? Colors.FromRGBA(0, 0, 0, 32) : Colors.FromRGBA(0, 0, 0, 64);
                GUIRenderer.Rect(color, Rect.FromSize(pos, new Vec2I(38, 38)));

                Image mini = _selectedBoxMinis[i];
                if (mini is null)
                {
                    continue;
                }
                mini.Render(pos.Plus(3, 3));
            }

            // Dim the side we're not using
            if (_isOnParty)
            {
                GUIRenderer.Rect(Colors.FromRGBA(0, 0, 0, 128), Rect.FromSize(Vec2I.FromRelative(0.48f, 0f, _renderSize), Vec2I.FromRelative(0.52f, 1f, _renderSize)));
            }
            else if (_partyVisible)
            {
                GUIRenderer.Rect(Colors.FromRGBA(0, 0, 0, 128), Rect.FromSize(new Vec2I(0, 0), Vec2I.FromRelative(0.48f, 1f, _renderSize)));
            }

            Window.RenderAll();
        }
    }
}
