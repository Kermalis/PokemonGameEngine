using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Player;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Transitions;
using Silk.NET.OpenGL;
using System;

namespace Kermalis.PokemonGameEngine.Render.Player
{
    internal sealed partial class BagGUI
    {
        private const int NUM_COLS = 4;
        private const int NUM_ROWS = 5;
        private const int BUTTON_SPACING_X = 2;
        private const int BUTTON_SPACING_Y = 2;
        private const int TOP_HEIGHT = 36;
        private const int POUCHES_START_X = 160;
        private const int BOTTOM_HEIGHT = 30;

        private static readonly Vec2I _renderSize = new(480, 270); // 16:9

        private readonly FrameBuffer2DColor _frameBuffer;
        private readonly TripleColorBackground _tripleColorBG;

        private readonly PlayerInventory _inv;

        private ITransition _transition;
        private Action _onClosed;

        private readonly BagGUIPouchButton[] _pouchButtons;
        private readonly BagGUIItemButton[] _itemButtons;
        private InventoryPouch<InventorySlotNew> _curPouch;
        private int _curPouchNumPages;

        private GUIString _curPouchName;
        private GUIString _itemPageStr;
        private readonly Image _newIcon;
        private readonly BagGUITextButton[] _pageButtons;
        private readonly BagGUITextButton _cancelButton;

        public BagGUI(PlayerInventory inv, Action onClosed)
        {
            Display.SetMinimumWindowSize(_renderSize);
            _frameBuffer = new FrameBuffer2DColor(_renderSize);

            _tripleColorBG = new TripleColorBackground();
            _tripleColorBG.SetColors(Colors.FromRGB(215, 230, 230), Colors.FromRGB(230, 165, 0), Colors.FromRGB(245, 180, 30));

            _inv = inv;

            // Create pouch buttons
            _pouchButtons = new BagGUIPouchButton[(int)ItemPouchType.MAX];
            for (int i = 0; i < _pouchButtons.Length; i++)
            {
                Vec2I pos = RenderUtils.DecideGridElementPos(new Vec2I(_renderSize.X - POUCHES_START_X, TOP_HEIGHT), new Vec2I(_pouchButtons.Length, 1), new Vec2I(2, 4), i);
                pos.X += POUCHES_START_X;
                _pouchButtons[i] = new BagGUIPouchButton((ItemPouchType)i, pos);
            }

            // Create item buttons
            var itemSpace = new Vec2I(_renderSize.X, _renderSize.Y - TOP_HEIGHT - BOTTOM_HEIGHT);
            Vec2I buttonSize = RenderUtils.DecideGridElementSize(itemSpace, new Vec2I(NUM_COLS, NUM_ROWS), new Vec2I(BUTTON_SPACING_X, BUTTON_SPACING_Y));
            _itemButtons = new BagGUIItemButton[NUM_COLS * NUM_ROWS];
            for (int i = 0; i < NUM_COLS * NUM_ROWS; i++)
            {
                Vec2I topLeft = RenderUtils.DecideGridElementPos(itemSpace, new Vec2I(NUM_COLS, NUM_ROWS), new Vec2I(BUTTON_SPACING_X, BUTTON_SPACING_Y), i);
                topLeft.Y += TOP_HEIGHT;
                _itemButtons[i] = new BagGUIItemButton(new Vec2I(i % NUM_COLS, i / NUM_COLS), Rect.FromSize(topLeft, buttonSize));
            }

            _selectedItem = _itemButtons[0];
            LoadSelectedPouch();

            // Create page buttons
            _pageButtons = new BagGUITextButton[2];
            var strPos = new Vec2I(6, 2);
            var rect = Rect.FromSize(new Vec2I(10, _renderSize.Y - 25), new Vec2I(19, 20));
            _pageButtons[0] = new BagGUITextButton("←", strPos, rect, PageButton_Left);
            rect = Rect.FromSize(new Vec2I(39, _renderSize.Y - 25), new Vec2I(19, 20));
            _pageButtons[1] = new BagGUITextButton("→", strPos, rect, PageButton_Right);

            // Cancel button
            strPos = new Vec2I(17, 5);
            rect = Rect.FromSize(new Vec2I(408, _renderSize.Y - 28), new Vec2I(70, 26));
            _cancelButton = new BagGUITextButton("CANCEL", strPos, rect, null);

            _newIcon = Image.LoadOrGet(AssetLoader.GetPath(@"Sprites\NewItem.png"));

            _onClosed = onClosed;

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInBag);
        }

        private void LoadSelectedPouch()
        {
            _curPouchName?.Delete();
            _curPouchName = new GUIString(_selectedPouch.ToString(), Font.Default, FontColors.DefaultDarkGray_I, scale: 2);
            _curPouch = _inv[_selectedPouch];
            if (_curPouch.Count == 0)
            {
                _curPouchNumPages = 1;
            }
            else
            {
                _curPouchNumPages = ((_curPouch.Count - 1) / (NUM_COLS * NUM_ROWS)) + 1;
            }
            LoadItemPage(0);
        }
        private void LoadItemPage(int page)
        {
            _itemPage = page;
            _itemPageStr?.Delete();
            _itemPageStr = new GUIString(string.Format("Page {0}/{1}", page + 1, _curPouchNumPages), Font.Default, FontColors.DefaultWhite_I);
            LoadItems();
        }
        private void LoadItems()
        {
            for (int i = 0; i < NUM_COLS * NUM_ROWS; i++)
            {
                int pageI = (NUM_COLS * NUM_ROWS * _itemPage) + i;
                _itemButtons[i].SetSlot(pageI < _curPouch.Count ? _curPouch[pageI] : null);
            }

            // Validate cursor location
            if (_selectedItem.Slot is null)
            {
                _selectedItem = _itemButtons[0];
                if (_cursorAt == CursorPos.Items && _selectedItem.Slot is null) // Empty pouch
                {
                    _cursorAt = CursorPos.Pouches;
                }
            }
        }

        private void SetExitFadeOutCallback()
        {
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutBag);
        }

        private void CB_FadeInBag()
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
        private void CB_FadeOutBag()
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
            _newIcon.DeductReference();
            _curPouchName.Delete();
            _itemPageStr.Delete();
            _cancelButton.Delete();
            for (int i = 0; i < _pouchButtons.Length; i++)
            {
                _pouchButtons[i].Delete();
            }
            for (int i = 0; i < 2; i++)
            {
                _pageButtons[i].Delete();
            }
            for (int i = 0; i < NUM_COLS * NUM_ROWS; i++)
            {
                _itemButtons[i].Delete();
            }
            _onClosed();
            _onClosed = null;
        }

        private void Render()
        {
            GL gl = Display.OpenGL;
            _frameBuffer.UseAndViewport(gl);
            _tripleColorBG.Render(gl); // No need to glClear since this overwrites everything

            // Draw pouch tabs background
            GUIRenderer.Rect(Colors.V4FromRGB(255, 255, 255), Rect.FromSize(new Vec2I(0, 0), new Vec2I(_renderSize.X, TOP_HEIGHT - 20)));
            GUIRenderer.Rect(Colors.V4FromRGB(230, 230, 230), Rect.FromSize(new Vec2I(0, TOP_HEIGHT - 20), new Vec2I(_renderSize.X, 2)));
            GUIRenderer.Rect(Colors.V4FromRGB(210, 210, 210), Rect.FromSize(new Vec2I(0, TOP_HEIGHT - 18), new Vec2I(_renderSize.X, 14)));
            GUIRenderer.Rect(Colors.V4FromRGB(120, 120, 120), Rect.FromSize(new Vec2I(0, TOP_HEIGHT - 4), new Vec2I(_renderSize.X, 2)));
            GUIRenderer.Rect(Colors.V4FromRGB(80, 80, 80), Rect.FromSize(new Vec2I(0, TOP_HEIGHT - 2), new Vec2I(_renderSize.X, 2)));

            // Draw pouch name
            _curPouchName.Render(new Vec2I(10, 0));
            // Draw page text
            _itemPageStr.Render(new Vec2I(75, _renderSize.Y - 20));

            // Draw pouch buttons
            for (int i = 0; i < _pouchButtons.Length; i++)
            {
                _pouchButtons[i].Render(_cursorAt == CursorPos.Pouches && (int)_selectedPouch == i);
            }

            // Draw page buttons
            for (int i = 0; i < 2; i++)
            {
                _pageButtons[i].Render(_cursorAt == CursorPos.Pages && _selectedPageButton == i);
            }

            // Draw cancel button
            _cancelButton.Render(_cursorAt == CursorPos.Cancel);

            // Draw item list
            for (int i = 0; i < NUM_COLS * NUM_ROWS; i++)
            {
                BagGUIItemButton button = _itemButtons[i];
                button.Render(_newIcon, _cursorAt == CursorPos.Items && _selectedItem == button);
            }
        }
    }
}
