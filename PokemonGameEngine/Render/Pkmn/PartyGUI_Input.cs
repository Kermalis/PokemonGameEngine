using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render.Battle;

namespace Kermalis.PokemonGameEngine.Render.Pkmn
{
    internal sealed partial class PartyGUI
    {
        private enum CursorPos : byte
        {
            Party,
            Back
        }

        private readonly bool _allowBack;
        private CursorPos _cursor = CursorPos.Party;
        private Vec2I _selectedMon;

        private static int SelectionCoordsToPartyIndex(int col, int row)
        {
            return (row * NUM_COLS) + col;
        }

        private void CB_HandleInputs()
        {
            Render();
            _frameBuffer.BlitToScreen();

            switch (_cursor)
            {
                case CursorPos.Party: HandleInputs_Party(); break;
                case CursorPos.Back: HandleInputs_Back(); break;
            }
        }

        private void HandleInputs_Party()
        {
            // Select a pkmn
            if (InputManager.JustPressed(Key.A))
            {
                BringUpPkmnActions(SelectionCoordsToPartyIndex(_selectedMon.X, _selectedMon.Y));
                return;
            }
            // Close menu or go back a pkmm
            if (InputManager.JustPressed(Key.B))
            {
                if (_mode == Mode.BattleReplace)
                {
                    SwitchesBuilder sb = BattleGUI.Instance.SwitchesBuilder;
                    if (sb.CanPop())
                    {
                        sb.Pop();
                        UpdateColors();
                    }
                }
                else if (_allowBack)
                {
                    ClosePartyMenu();
                }
                return;
            }
            if (InputManager.JustPressed(Key.Left))
            {
                if (_selectedMon.X > 0)
                {
                    _selectedMon.X--;
                    UpdateBounces(_selectedMon.X + 1, _selectedMon.Y);
                }
                return;
            }
            if (InputManager.JustPressed(Key.Right))
            {
                // Disallow moving into an empty slot
                if (_selectedMon.X < NUM_COLS - 1
                    && SelectionCoordsToPartyIndex(_selectedMon.X + 1, _selectedMon.Y) < _members.Count)
                {
                    _selectedMon.X++;
                    UpdateBounces(_selectedMon.X - 1, _selectedMon.Y);
                }
                return;
            }
            if (InputManager.JustPressed(Key.Down))
            {
                if (SelectionCoordsToPartyIndex(_selectedMon.X, _selectedMon.Y + 1) < _members.Count)
                {
                    _selectedMon.Y++;
                    UpdateBounces(_selectedMon.X, _selectedMon.Y - 1);
                }
                else if (_allowBack)
                {
                    _cursor = CursorPos.Back;
                    UpdateBounces(_selectedMon.X, _selectedMon.Y);
                }
                return;
            }
            if (InputManager.JustPressed(Key.Up))
            {
                if (_selectedMon.Y > 0)
                {
                    _selectedMon.Y--;
                    UpdateBounces(_selectedMon.X, _selectedMon.Y + 1);
                }
                return;
            }
        }
        private void HandleInputs_Back()
        {
            if (InputManager.JustPressed(Key.A) || InputManager.JustPressed(Key.B))
            {
                ClosePartyMenu();
                return;
            }
            if (InputManager.JustPressed(Key.Up))
            {
                _cursor = CursorPos.Party;
                UpdateBounces(_selectedMon.X, _selectedMon.Y);
                return;
            }
        }
    }
}
