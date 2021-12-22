using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.OpenGL;
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
            _quantityWidth = (int)font.MeasureString(q).Width;
        }

        public void Render(bool isSelected, int x1, int x2, int y, int height, int xOfs)
        {
            if (isSelected)
            {
                GUIRenderer.Instance.FillRectangle(Colors.FromRGBA(255, 0, 0, 128), new Rect2D(new Pos2D(x1, y), new Pos2D(x2, y + height - 1)));
            }
            x1 += xOfs;
            x2 -= xOfs;
            _itemName.Render(new Pos2D(x1, y));
            _quantityStr.Render(new Pos2D(x2 - _quantityWidth, y));
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
        public float X2;
        public float Y2;

        public Vector4 BackColor;
        public Vector4 BorderColor;

        public ItemGUIChoices(float x1, float y1, float x2, float y2, float spacing,
            in Vector4 backColor, in Vector4 borderColor,
            Action backCommand = null)
            : base(x1, y1, spacing, backCommand: backCommand)
        {
            X2 = x2;
            Y2 = y2;
            BackColor = backColor;
            BorderColor = borderColor;
        }

        public override void Render()
        {
            Size2D dstSize = FrameBuffer.Current.Size;
            int x1 = (int)(X * dstSize.Width);
            float fy1 = Y * dstSize.Height;
            int y1 = (int)fy1;
            int x2 = (int)(X2 * dstSize.Width);
            int y2 = (int)(Y2 * dstSize.Height);

            // Draw background
            GUIRenderer.Instance.FillRectangle(BackColor, new Rect2D(new Pos2D(x1, y1), new Pos2D(x2, y2))); // TODO: Rounded 10
            GUIRenderer.Instance.DrawRectangle(BorderColor, new Rect2D(new Pos2D(x1, y1), new Pos2D(x2, y2))); // TODO: Rounded 10

            int height = (int)(dstSize.Height * Spacing);
            int xOfs = (int)(0.015f * dstSize.Width);
            float yOfs = 0.015f * dstSize.Height;
            float space = Spacing * dstSize.Height;
            int count = _choices.Count;
            for (int i = 0; i < count; i++)
            {
                ItemGUIChoice c = _choices[i];
                bool isSelected = Selected == i;
                int y = (int)(fy1 + (space * i) + yOfs);
                c.Render(isSelected, x1, x2, y, height, xOfs);
            }
        }
    }
}
