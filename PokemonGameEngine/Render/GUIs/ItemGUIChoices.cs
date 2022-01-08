using Kermalis.PokemonGameEngine.Item;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    internal sealed class ItemGUIChoice : GUIChoice
    {
        private readonly GUIString _itemName;
        private readonly GUIString _quantityStr;
        private readonly int _quantityWidth;

        public ItemGUIChoice(InventorySlot slot, Action command, bool isEnabled = true)
            : base(command, isEnabled: isEnabled)
        {
            Font font = Font.Default;
            Vector4[] colors = FontColors.DefaultDarkGray_I;
            _itemName = new GUIString(ItemData.GetItemName(slot.Item), font, colors);
            string q = "x" + slot.Quantity.ToString();
            _quantityStr = new GUIString(q, font, colors);
            _quantityWidth = font.GetSize(q).X;
        }

        public void Render(bool isSelected, int x1, int x2, int y, int height, int xOfs)
        {
            if (isSelected)
            {
                GUIRenderer.Instance.FillRectangle(Colors.FromRGBA(255, 0, 0, 128), Rect.FromCorners(new Vec2I(x1, y), new Vec2I(x2, y + height - 1)));
            }
            x1 += xOfs;
            x2 -= xOfs;
            _itemName.Render(new Vec2I(x1, y));
            _quantityStr.Render(new Vec2I(x2 - _quantityWidth, y));
        }

        public override void Dispose()
        {
            Command = null;
            _itemName.Delete();
            _quantityStr.Delete();
        }
    }

    internal sealed class ItemGUIChoices : GUIChoices<ItemGUIChoice>
    {
        public Vector2 BottomRight;

        public Vector4 BackColor;
        public Vector4 BorderColor;

        public ItemGUIChoices(Vector2 topLeft, Vector2 bottomRight, float spacing,
            in Vector4 backColor, in Vector4 borderColor,
            Action backCommand = null)
            : base(topLeft, spacing, backCommand: backCommand)
        {
            BottomRight = bottomRight;
            BackColor = backColor;
            BorderColor = borderColor;
        }

        public override void Render(Vec2I viewSize)
        {
            Vec2I topLeft;
            topLeft.X = (int)(Pos.X * viewSize.X);
            float tlY = Pos.Y * viewSize.Y;
            topLeft.Y = (int)tlY;
            var bottomRight = (Vec2I)(BottomRight * viewSize);

            // Draw background
            GUIRenderer.Instance.FillRectangle(BackColor, Rect.FromCorners(topLeft, bottomRight)); // TODO: Rounded 10
            GUIRenderer.Instance.DrawRectangle(BorderColor, Rect.FromCorners(topLeft, bottomRight)); // TODO: Rounded 10

            int height = (int)(viewSize.Y * Spacing);
            int xOfs = (int)(0.015f * viewSize.X);
            float yOfs = 0.015f * viewSize.Y;
            float space = Spacing * viewSize.Y;
            int count = _choices.Count;
            for (int i = 0; i < count; i++)
            {
                ItemGUIChoice c = _choices[i];
                bool isSelected = Selected == i;
                int y = (int)(tlY + (space * i) + yOfs);
                c.Render(isSelected, topLeft.X, bottomRight.X, y, height, xOfs);
            }
        }
    }
}
