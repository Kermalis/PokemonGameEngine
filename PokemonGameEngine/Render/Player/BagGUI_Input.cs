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

        private CursorPos _cursorAt = CursorPos.Items;
        private BagGUIItemButton _selectedItem;
        private int _itemPage;
        private int _selectedPageButton = 1;

        private BagGUIItemButton GetItemButton(Vec2I pos)
        {
            return _itemButtons[(pos.Y * NUM_COLS) + pos.X];
        }

        private void CB_HandleInputs()
        {
            Render();
            _frameBuffer.BlitToScreen();

            if (InputManager.CursorMode)
            {
                HandleCursorInputs();
            }
            else
            {
                switch (_cursorAt)
                {
                    case CursorPos.Pouches: HandleButtonInputs_Pouches(); break;
                    case CursorPos.Items: HandleButtonInputs_Items(); break;
                    case CursorPos.Pages: HandleButtonInputs_Pages(); break;
                    case CursorPos.Cancel: HandleButtonInputs_Cancel(); break;
                }
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

        private void HandleCursorInputs()
        {
            // Try to select an item button
            for (int i = 0; i < NUM_COLS * NUM_ROWS; i++)
            {
                BagGUIItemButton button = _itemButtons[i];
                if (button.Slot is null)
                {
                    break;
                }
                if (_cursorAt == CursorPos.Items && button == _selectedItem)
                {
                    continue; // Don't handle if it's already selected
                }
                if (button.IsHovering())
                {
                    _cursorAt = CursorPos.Items;
                    _selectedItem = _itemButtons[i];
                    return;
                }
            }
            // Try to select a pouch button
            for (int i = 0; i < _pouchButtons.Length; i++)
            {
                if (_cursorAt == CursorPos.Pouches && i == (int)_selectedPouch)
                {
                    continue; // Don't handle if it's already selected
                }
                BagGUIPouchButton button = _pouchButtons[i];
                if (button.IsHovering())
                {
                    _cursorAt = CursorPos.Pouches;
                    _selectedPouch = (ItemPouchType)i;
                    LoadSelectedPouch();
                    return;
                }
            }
            // Try to select a page button
            for (int i = 0; i < _pageButtons.Length; i++)
            {
                if (_cursorAt == CursorPos.Pages && i == _selectedPageButton)
                {
                    continue; // Don't handle if it's already selected
                }
                BagGUITextButton button = _pageButtons[i];
                if (button.IsHovering())
                {
                    _cursorAt = CursorPos.Pages;
                    _selectedPageButton = i;
                    return;
                }
            }
            // Try to select cancel button
            if (_cursorAt != CursorPos.Cancel && _cancelButton.IsHovering())
            {
                _cursorAt = CursorPos.Cancel;
                return;
            }
            // Try to click the selected item button
            if (_selectedItem.Slot is not null && _selectedItem.JustPressedCursor())
            {
                _cursorAt = CursorPos.Items;
                _selectedItem.Press();
                return;
            }
            // Try to click the selected page button
            if (_pageButtons[_selectedPageButton].JustPressedCursor())
            {
                _cursorAt = CursorPos.Pages;
                _pageButtons[_selectedPageButton].OnPress();
                return;
            }
            // Try to click cancel button
            if (_cancelButton.JustPressedCursor())
            {
                _cursorAt = CursorPos.Cancel;
                SetExitFadeOutCallback();
                return;
            }
        }
        private void HandleButtonInputs_Pouches()
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
                    _cursorAt = CursorPos.Cancel;
                }
                else
                {
                    _cursorAt = CursorPos.Items;
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
        private void HandleButtonInputs_Items()
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
                if (_selectedItem.GridPos.Y == 0)
                {
                    _cursorAt = CursorPos.Pouches;
                }
                else
                {
                    _selectedItem = GetItemButton(_selectedItem.GridPos.Plus(0, -1));
                }
                return;
            }
            if (InputManager.JustPressed(Key.Down))
            {
                if (_selectedItem.GridPos.Y < NUM_ROWS - 1)
                {
                    // Disallow moving into an empty slot
                    BagGUIItemButton newButton = GetItemButton(_selectedItem.GridPos.Plus(0, 1));
                    if (newButton.Slot is not null)
                    {
                        _selectedItem = newButton;
                        return;
                    }
                }
                _cursorAt = CursorPos.Pages;
                return;
            }
            if (InputManager.JustPressed(Key.Left))
            {
                if (_selectedItem.GridPos.X > 0)
                {
                    _selectedItem = GetItemButton(_selectedItem.GridPos.Plus(-1, 0));
                }
                return;
            }
            if (InputManager.JustPressed(Key.Right))
            {
                if (_selectedItem.GridPos.X < NUM_COLS - 1)
                {
                    // Disallow moving into an empty slot
                    BagGUIItemButton newButton = GetItemButton(_selectedItem.GridPos.Plus(1, 0));
                    if (newButton.Slot is not null)
                    {
                        _selectedItem = newButton;
                    }
                }
                return;
            }
            if (InputManager.JustPressed(Key.A))
            {
                _selectedItem.Press();
                return;
            }
        }
        private void HandleButtonInputs_Pages()
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
                _cursorAt = CursorPos.Items;
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
                    _cursorAt = CursorPos.Cancel;
                }
                return;
            }
            if (InputManager.JustPressed(Key.A))
            {
                _pageButtons[_selectedPageButton].OnPress();
                return;
            }
        }
        private void HandleButtonInputs_Cancel()
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
                _cursorAt = CursorPos.Pages;
                return;
            }
            if (InputManager.JustPressed(Key.Up))
            {
                if (_itemButtons[0].Slot is null) // Empty pouch
                {
                    _cursorAt = CursorPos.Pouches;
                }
                else
                {
                    _cursorAt = CursorPos.Items;
                }
                return;
            }
        }
    }
}
