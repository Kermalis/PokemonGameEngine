using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Item;

namespace Kermalis.PokemonGameEngine.Render.Player
{
    internal sealed partial class BagGUI
    {
        private enum CursorPos : byte
        {
            Pouches,
            Items,
            Pages,
            Cancel
        }

        private static ItemPouchType _selectedPouch = ItemPouchType.Items; // static so it remembers when you close the bag

        private CursorPos _cursor = CursorPos.Items;
        private Vec2I _selectedItem;
        private int _itemPage;
        private int _selectedPageButton = 1;

        private static int SelectionCoordsToItemButtonIndex(int col, int row)
        {
            return (row * NUM_COLS) + col;
        }

        private void CB_HandleInputs()
        {
            Render();
            _frameBuffer.BlitToScreen();

            switch (_cursor)
            {
                case CursorPos.Pouches: HandleInputs_Pouches(); break;
                case CursorPos.Items: HandleInputs_Items(); break;
                case CursorPos.Pages: HandleInputs_Pages(); break;
                case CursorPos.Cancel: HandleInputs_Cancel(); break;
            }
        }

        private void PageButton_Left()
        {
            if (_itemPage > 0)
            {
                LoadItemPage(_itemPage - 1);
            }
        }
        private void PageButton_Right()
        {
            if (_itemPage < _curPouchNumPages - 1)
            {
                LoadItemPage(_itemPage + 1);
            }
        }

        private bool HandleLRPouchChange()
        {
            if (InputManager.JustPressed(Key.L))
            {
                if (_selectedPouch > 0)
                {
                    _selectedPouch--;
                    LoadSelectedPouch();
                }
                return true;
            }
            if (InputManager.JustPressed(Key.R))
            {
                if (_selectedPouch < ItemPouchType.MAX - 1)
                {
                    _selectedPouch++;
                    LoadSelectedPouch();
                }
                return true;
            }
            return false;
        }
        private bool HandleXYPageChange()
        {
            if (InputManager.JustPressed(Key.X))
            {
                _pageButtons[0].OnPress();
                return true;
            }
            if (InputManager.JustPressed(Key.Y))
            {
                _pageButtons[1].OnPress();
                return true;
            }
            return false;
        }
        private bool HandleExitPress()
        {
            if (InputManager.JustPressed(Key.B))
            {
                SetExitFadeOutCallback();
                return true;
            }
            return false;
        }

        private void HandleInputs_Pouches()
        {
            if (HandleExitPress())
            {
                return;
            }
            if (HandleLRPouchChange())
            {
                return;
            }
            if (HandleXYPageChange())
            {
                return;
            }
            if (InputManager.JustPressed(Key.Down))
            {
                if (_itemButtons[0].Slot is null) // Empty pouch
                {
                    _cursor = CursorPos.Cancel;
                }
                else
                {
                    _cursor = CursorPos.Items;
                }
                return;
            }
            if (InputManager.JustPressed(Key.Left))
            {
                if (_selectedPouch > 0)
                {
                    _selectedPouch--;
                    LoadSelectedPouch();
                }
                return;
            }
            if (InputManager.JustPressed(Key.Right))
            {
                if (_selectedPouch < ItemPouchType.MAX - 1)
                {
                    _selectedPouch++;
                    LoadSelectedPouch();
                }
                return;
            }
        }
        private void HandleInputs_Items()
        {
            if (HandleExitPress())
            {
                return;
            }
            if (HandleLRPouchChange())
            {
                return;
            }
            if (HandleXYPageChange())
            {
                return;
            }
            if (InputManager.JustPressed(Key.Up))
            {
                if (_selectedItem.Y == 0)
                {
                    _cursor = CursorPos.Pouches;
                }
                else
                {
                    _selectedItem.Y--;
                }
                return;
            }
            if (InputManager.JustPressed(Key.Down))
            {
                if (_selectedItem.Y < NUM_ROWS - 1)
                {
                    // Disallow moving into an empty slot
                    if (_itemButtons[SelectionCoordsToItemButtonIndex(_selectedItem.X, _selectedItem.Y + 1)].Slot is null)
                    {
                        _cursor = CursorPos.Pages;
                    }
                    else
                    {
                        _selectedItem.Y++;
                    }
                }
                else
                {
                    _cursor = CursorPos.Pages;
                }
                return;
            }
            if (InputManager.JustPressed(Key.Left))
            {
                if (_selectedItem.X > 0)
                {
                    _selectedItem.X--;
                }
                return;
            }
            if (InputManager.JustPressed(Key.Right))
            {
                if (_selectedItem.X < NUM_COLS - 1)
                {
                    // Disallow moving into an empty slot
                    if (_itemButtons[SelectionCoordsToItemButtonIndex(_selectedItem.X + 1, _selectedItem.Y)].Slot is not null)
                    {
                        _selectedItem.X++;
                    }
                }
                return;
            }
            // TODO: A
        }
        private void HandleInputs_Pages()
        {
            if (HandleExitPress())
            {
                return;
            }
            if (HandleLRPouchChange())
            {
                return;
            }
            if (HandleXYPageChange())
            {
                return;
            }
            if (InputManager.JustPressed(Key.Up))
            {
                _cursor = CursorPos.Items;
                return;
            }
            if (InputManager.JustPressed(Key.Left))
            {
                if (_selectedPageButton == 1)
                {
                    _selectedPageButton = 0;
                }
                return;
            }
            if (InputManager.JustPressed(Key.Right))
            {
                if (_selectedPageButton == 0)
                {
                    _selectedPageButton = 1;
                }
                else
                {
                    _cursor = CursorPos.Cancel;
                }
                return;
            }
            if (InputManager.JustPressed(Key.A))
            {
                _pageButtons[_selectedPageButton].OnPress();
                return;
            }
        }
        private void HandleInputs_Cancel()
        {
            if (InputManager.JustPressed(Key.A) || InputManager.JustPressed(Key.B))
            {
                SetExitFadeOutCallback();
                return;
            }
            if (HandleLRPouchChange())
            {
                return;
            }
            if (HandleXYPageChange())
            {
                return;
            }
            if (InputManager.JustPressed(Key.Left))
            {
                _cursor = CursorPos.Pages;
                return;
            }
            if (InputManager.JustPressed(Key.Up))
            {
                if (_itemButtons[0].Slot is null) // Empty pouch
                {
                    _cursor = CursorPos.Pouches;
                }
                else
                {
                    _cursor = CursorPos.Items;
                }
                return;
            }
        }
    }
}
