using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    internal sealed class PartyGUI
    {
        public enum Mode : byte
        {
            PkmnMenu,
            SelectDaycare
        }

        private readonly Mode _mode;
        private readonly Party _party;
        private readonly List<PartyGUIMember> _members;
        private readonly List<Sprite> _sprites;

        private FadeColorTransition _fadeTransition;
        private Action _onClosed;

        private Window _textChoicesWindow;
        private TextGUIChoices _textChoices;
        private string _message;

        private int _selectionX;
        private int _selectionY;

        #region Open & Close GUI

        public unsafe PartyGUI(Party party, Mode mode, Action onClosed)
        {
            _mode = mode;
            _party = party;
            _members = new List<PartyGUIMember>(PkmnConstants.PartyCapacity);
            _sprites = new List<Sprite>();
            foreach (PartyPokemon pkmn in party)
            {
                _members.Add(new PartyGUIMember(pkmn, _sprites));
            }
            _members[0].SetBigBounce();

            _onClosed = onClosed;
            _fadeTransition = new FadeFromColorTransition(20, 0);
            Game.Instance.SetCallback(CB_FadeInParty);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void ClosePartyMenu()
        {
            _fadeTransition = new FadeToColorTransition(20, 0);
            Game.Instance.SetCallback(CB_FadeOutParty);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void CB_FadeInParty()
        {
            Sprite.DoCallbacks(_sprites);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Game.Instance.SetCallback(CB_LogicTick);
                Game.Instance.SetRCallback(RCB_RenderTick);
            }
        }
        private unsafe void CB_FadeOutParty()
        {
            Sprite.DoCallbacks(_sprites);
            if (_fadeTransition.IsDone)
            {
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

        private int SelectionCoordsToPartyIndex(int col, int row)
        {
            if (row == -1)
            {
                return -1;
            }
            int i = row * 2 + col;
            if (i >= _party.Count)
            {
                return -1;
            }
            return i;
        }
        private void UpdateBounces(int oldCol, int oldRow)
        {
            int i = SelectionCoordsToPartyIndex(oldCol, oldRow);
            if (i != -1)
            {
                _members[i].SetSmallBounce();
            }
            i = SelectionCoordsToPartyIndex(_selectionX, _selectionY);
            if (i != -1)
            {
                _members[i].SetBigBounce();
            }
        }

        private void Action_SelectPartyPkmn(PartyPokemon pkmn)
        {
            switch (_mode)
            {
                case Mode.SelectDaycare:
                {
                    short index = (short)_party.IndexOf(pkmn);
                    Game.Instance.Save.Vars[Var.SpecialVar_Result] = index;
                    CloseChoices();
                    ClosePartyMenu();
                    return;
                }
                default: throw new Exception();
            }
        }
        private void Action_BringUpSummary(PartyPokemon pkmn)
        {

        }

        private void BringUpPkmnActions(PartyPokemon pkmn)
        {
            _textChoices = new TextGUIChoices(0, 0, backCommand: CloseChoicesThenGoToLogicTick, font: Font.Default, fontColors: Font.DefaultDark, selectedColors: Font.DefaultSelected);
            switch (_mode)
            {
                case Mode.PkmnMenu:
                {
                    _textChoices.Add(new TextGUIChoice("Summary", () => Action_BringUpSummary(pkmn)));
                    _textChoices.Add(new TextGUIChoice("Cancel", CloseChoicesThenGoToLogicTick));
                    break;
                }
                case Mode.SelectDaycare:
                {
                    Game.Instance.Save.Vars[Var.SpecialVar_Result] = -1; // If you back out, the default selection is -1
                    if (!pkmn.IsEgg)
                    {
                        _textChoices.Add(new TextGUIChoice("Select", () => Action_SelectPartyPkmn(pkmn)));
                    }
                    _textChoices.Add(new TextGUIChoice("Cancel", CloseChoicesThenGoToLogicTick));
                    break;
                }
                default: throw new Exception();
            }
            _textChoices.GetSize(out int width, out int height);
            _textChoicesWindow = new Window(0.6f, 0.3f, width, height, RenderUtils.Color(255, 255, 255, 255));
            RenderChoicesOntoWindow();
            _message = string.Format("Do what with {0}?", pkmn.Nickname);
            Game.Instance.SetCallback(CB_Choices);
        }
        private void CloseChoices()
        {
            _textChoicesWindow.Close();
            _textChoicesWindow = null;
            _textChoices.Dispose();
            _textChoices = null;
        }
        private void CloseChoicesThenGoToLogicTick()
        {
            CloseChoices();
            _message = null;
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

        private void CB_Choices()
        {
            Sprite.DoCallbacks(_sprites);
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
        private void CB_LogicTick()
        {
            Sprite.DoCallbacks(_sprites);
            if (InputManager.IsPressed(Key.A))
            {
                if (_selectionY == -1)
                {
                    ClosePartyMenu();
                }
                else
                {
                    int i = SelectionCoordsToPartyIndex(_selectionX, _selectionY);
                    BringUpPkmnActions(_party[i]);
                }
                return;
            }
            if (InputManager.IsPressed(Key.B))
            {
                ClosePartyMenu();
                return;
            }
            if (InputManager.IsPressed(Key.Left))
            {
                if (_selectionX == 1)
                {
                    _selectionX = 0;
                    UpdateBounces(1, _selectionY);
                }
                return;
            }
            if (InputManager.IsPressed(Key.Right))
            {
                if (_selectionX == 0 && SelectionCoordsToPartyIndex(1, _selectionY) != -1)
                {
                    _selectionX = 1;
                    UpdateBounces(0, _selectionY);
                }
                return;
            }
            if (InputManager.IsPressed(Key.Down))
            {
                int oldY = _selectionY;
                if (oldY != -1)
                {
                    if (SelectionCoordsToPartyIndex(_selectionX, oldY + 1) == -1)
                    {
                        _selectionY = -1;
                    }
                    else
                    {
                        _selectionY++;
                    }
                    UpdateBounces(_selectionX, oldY);
                }
                return;
            }
            if (InputManager.IsPressed(Key.Up))
            {
                int oldY = _selectionY;
                if (oldY == -1)
                {
                    _selectionY = (_party.Count - 1) / 2;
                    UpdateBounces(_selectionX, oldY);
                }
                else if (oldY > 0)
                {
                    _selectionY--;
                    UpdateBounces(_selectionX, oldY);
                }
                return;
            }
        }

        private unsafe void RCB_RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            // Background
            RenderUtils.ThreeColorBackground(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(222, 50, 60, 255), RenderUtils.Color(190, 40, 50, 255), RenderUtils.Color(255, 180, 200, 255));

            for (int i = 0; i < _members.Count; i++)
            {
                int col = i % 2;
                int row = i / 2;
                int x = col == 0 ? bmpWidth / 40 : (bmpWidth / 2) + (bmpWidth / 40);
                int y = row * (bmpHeight / 4) + (bmpHeight / 20);
                _members[i].Render(bmpAddress, bmpWidth, bmpHeight, x, y, col == _selectionX && row == _selectionY);
            }

            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.5f, 0.8f, 0.5f, 0.2f, _selectionY == -1 ? RenderUtils.Color(96, 48, 48, 255) : RenderUtils.Color(48, 48, 48, 255));
            Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, 0.5f, 0.8f, "Back", Font.DefaultWhite);

            if (_message != null)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0, 0.8f, 0.5f, 0.2f, RenderUtils.Color(200, 200, 200, 255));
                Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, 0, 0.8f, _message, Font.DefaultDark);
            }

            Game.Instance.RenderWindows(bmpAddress, bmpWidth, bmpHeight);
        }
    }
}
