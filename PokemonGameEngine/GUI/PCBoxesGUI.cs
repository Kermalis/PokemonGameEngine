using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;
using System;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class PCBoxesGUI
    {
        private const int NumPerRow = 5;
        private const int NumColumns = PkmnConstants.BoxCapacity / NumPerRow; // Won't work if it's not evenly divisible

        private readonly PCBoxes _boxes;
        private readonly Party _party;

        private FadeColorTransition _fadeTransition;
        private Action _onClosed;

        private bool _isOnParty = false;
        private bool _partyVisible = false;
        private readonly PartyPkmnGUIChoices _partyChoices;

        private int _selectedBox;
        private int _selectedRow;
        private int _selectedCol;
        private Sprite[] _selectedBoxMinis;
        private AnimatedSprite _selectedMainSprite;

        public unsafe PCBoxesGUI(PCBoxes boxes, Party party, Action onClosed)
        {
            _boxes = boxes;
            _party = party;

            _partyChoices = new PartyPkmnGUIChoices(0.03f, 0.18f, 0.47f, 0.97f, 0.004f);
            LoadPartyChoices();
            LoadBoxContents();

            _onClosed = onClosed;
            _fadeTransition = new FadeFromColorTransition(20, 0);
            Game.Instance.SetCallback(CB_FadeInPC);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private void BringUpPartyPkmnActions(PartyPokemon pkmn)
        {
            int storedIn = _boxes.Add(pkmn);
            if (storedIn == -1)
            {
                throw new Exception();
            }
            _party.Remove(pkmn);
            DisposePartyChoices();
            LoadPartyChoices();
            LoadBoxContents();
            Console.WriteLine("{0} was stored in Box {1}", pkmn.Nickname, storedIn + 1);
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
            _selectedBoxMinis = new Sprite[PkmnConstants.BoxCapacity];
            for (int i = 0; i < PkmnConstants.BoxCapacity; i++)
            {
                BoxPokemon pkmn = _boxes[_selectedBox][i];
                if (pkmn is null)
                {
                    continue;
                }
                _selectedBoxMinis[i] = SpriteUtils.GetMinisprite(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny);
            }
            LoadPkmnContents(GetSelectedBoxPkmn());
        }
        private void LoadPkmnContents(BoxPokemon pkmn)
        {
            if (pkmn is null)
            {
                _selectedMainSprite = null;
                return;
            }
            _selectedMainSprite = SpriteUtils.GetPokemonSprite(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny, false, false, pkmn.PID);
        }

        private unsafe void CloseMenu()
        {
            _fadeTransition = new FadeToColorTransition(20, 0);
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
        private void CB_LogicTick()
        {
            if (InputManager.IsPressed(Key.B))
            {
                CloseMenu();
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
                BoxPokemon pkmn = GetSelectedBoxPkmn();
                _party.Add(new PartyPokemon(pkmn));
                _boxes[_selectedBox].Remove(pkmn);
                DisposePartyChoices();
                LoadPartyChoices();
                LoadBoxContents();
                Console.WriteLine("{0} was taken from Box {1}", pkmn.Nickname, _selectedBox + 1);
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

        private unsafe void RCB_Fading(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RCB_RenderTick(bmpAddress, bmpWidth, bmpHeight);
            _fadeTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
        }
        private unsafe void RCB_RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            // Background
            RenderUtils.ThreeColorBackground(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(215, 231, 230, 255), RenderUtils.Color(231, 163, 0, 255), RenderUtils.Color(242, 182, 32, 255));

            // PC
            Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, 0.02f, 0.01f, 2, $"BOX {_selectedBox + 1}", Font.DefaultDark);

            if (_partyVisible)
            {
                _partyChoices.Render(bmpAddress, bmpWidth, bmpHeight);
            }
            else if (_selectedMainSprite != null)
            {
                _selectedMainSprite.DrawOn(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * 0.24f) - (_selectedMainSprite.Width / 2), (int)(bmpHeight * 0.7f) - _selectedMainSprite.Height);
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

                Sprite mini = _selectedBoxMinis[i];
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
        }
    }
}
