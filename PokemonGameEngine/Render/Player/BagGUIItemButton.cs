using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Player
{
    internal sealed class BagGUIItemButton
    {
        private const float NEW_DISAPPEAR_TIME = 1f; // In seconds

        private readonly Rect _rect;
        public readonly Vec2I GridPos;

        public InventorySlotNew Slot;
        private Action _onPress;
        private GUIString _name;
        private GUIString _quantity;
        private Image _icon;

        private float _selectedTimer;

        public BagGUIItemButton(Vec2I gridPos, in Rect rect)
        {
            _rect = rect;
            GridPos = gridPos;
        }

        private void TestPress()
        {
            Console.WriteLine("Pressed at " + GridPos);
        }

        public void SetSlot(InventorySlotNew slot)
        {
            _selectedTimer = 0f;
            Slot = slot;
            Delete();
            if (slot is null)
            {
                return;
            }

            string str;
            int tmhmIndex = ItemData.GetTMIndex(slot.Item);
            if (tmhmIndex != -1) // is tm
            {
                str = PBEDataProvider.Instance.GetMoveName(ItemData.TMMoves[tmhmIndex]).English;
            }
            else
            {
                tmhmIndex = ItemData.GetHMIndex(slot.Item);
                if (tmhmIndex != -1) // is hm
                {
                    str = PBEDataProvider.Instance.GetMoveName(ItemData.HMMoves[tmhmIndex]).English;
                }
                else
                {
                    str = ItemData.GetItemName(slot.Item);
                }
            }
            Vec2I offset = Font.Default.GetSize(str); // Right align
            offset.Y = 0;
            _name = new GUIString(str, Font.Default, FontColors.DefaultDarkGray_I, pos: _rect.TopLeft - offset);

            if (tmhmIndex == -1) // Don't show quantity for TMs and HMs
            {
                str = 'x' + slot.Quantity.ToString();
                offset = Font.Default.GetSize(str); // Right align
                offset.Y = 0;
                _quantity = new GUIString(str, Font.Default, FontColors.DefaultDarkGray_I, pos: _rect.TopLeft - offset);
            }
            else
            {
                str = ItemData.GetItemName(slot.Item);
                offset = Font.Default.GetSize(str); // Right align
                offset.Y = 0;
                _quantity = new GUIString(str, Font.Default, FontColors.DefaultDarkGray_I, pos: _rect.TopLeft - offset);
            }

            _icon = Image.LoadOrGet(ItemData.GetItemIconAssetPath(slot.Item));

            _onPress = TestPress;
        }

        public bool IsHovering()
        {
            return InputManager.IsHovering(_rect, cornerRadii: new(7));
        }
        public bool JustPressedCursor()
        {
            return InputManager.JustPressed(_rect, cornerRadii: new(7));
        }
        public void Press()
        {
            Slot.New = false;
            _onPress();
        }

        public void Render(Image _new, bool isSelected)
        {
            if (Slot is null)
            {
                // Empty slot
                GUIRenderer.Rect(Colors.V4FromRGB(255, 200, 145), _rect, lineThickness: 2, cornerRadii: new(10));
                return;
            }

            // Draw rect
            Vec2I size = _rect.GetSize();
            GUIRenderer.Rect(Colors.V4FromRGB(240, 225, 255), Rect.FromSize(_rect.TopLeft, new Vec2I(size.X, 19)),
                cornerRadii: new(10, 0, 10, 0));
            GUIRenderer.Rect(Colors.V4FromRGB(210, 195, 215), Rect.FromSize(new Vec2I(_rect.TopLeft.X, _rect.TopLeft.Y + 19), new Vec2I(size.X, 1)));
            GUIRenderer.Rect(Colors.V4FromRGB(190, 175, 205), Rect.FromSize(new Vec2I(_rect.TopLeft.X, _rect.TopLeft.Y + 20), new Vec2I(size.X, size.Y - 20)),
                cornerRadii: new(0, 10, 0, 10));

            Vector4 lineColor;
            if (isSelected)
            {
                _selectedTimer += Display.DeltaTime;
                if (_selectedTimer >= NEW_DISAPPEAR_TIME)
                {
                    Slot.New = false;
                }
                // Animate selection color
                uint green = (uint)Utils.Lerp(120f, 180f, Easing.BellCurve2(_selectedTimer % 1f));
                lineColor = Colors.V4FromRGB(48, green, 255);
            }
            else
            {
                _selectedTimer = 0f;
                lineColor = Colors.V4FromRGB(75, 75, 75);
            }
            GUIRenderer.Rect(lineColor, _rect, lineThickness: 2, cornerRadii: new(10));

            // Draw icon
            _icon.Render(_rect.TopLeft + new Vec2I(3, 4));
            // Draw name
            _name.Render(new Vec2I(105, 4));
            // New mark
            if (Slot.New)
            {
                _new.Render(_rect.TopLeft + new Vec2I(30, 21));
            }
            // Draw quantity/tmhm
            _quantity.Render(new Vec2I(105, 19));
        }

        public void Delete()
        {
            _onPress = null;
            _name?.Delete();
            _name = null;
            _quantity?.Delete();
            _quantity = null;
            _icon?.DeductReference();
            _icon = null;
        }
    }
}
