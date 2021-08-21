using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Interactive
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
            ColorF[] colors = FontColors.DefaultDarkGray_I;
            _itemName = new GUIString(ItemData.GetItemName(slot.Item), font, colors);
            string q = "x" + slot.Quantity.ToString();
            _quantityStr = new GUIString(q, font, colors);
            _quantityWidth = (int)font.MeasureString(q).Width;
        }

        public void Render(GL gl, bool isSelected, int x1, int x2, int y, int height, int xOfs)
        {
            if (isSelected)
            {
                GUIRenderer.Instance.FillRectangle(ColorF.FromRGBA(255, 0, 0, 128), new Rect2D(new Pos2D(x1, y), new Pos2D(x2, y + height - 1)));
            }
            x1 += xOfs;
            x2 -= xOfs;
            _itemName.Render(gl, new Pos2D(x1, y));
            _quantityStr.Render(gl, new Pos2D(x2 - _quantityWidth, y));
        }

        public override void Dispose()
        {
            GL gl = Game.OpenGL;
            Command = null;
            _itemName.Delete(gl);
            _quantityStr.Delete(gl);
        }
    }

    internal sealed class ItemGUIChoices : GUIChoices<ItemGUIChoice>
    {
        public float X2;
        public float Y2;

        public ColorF BackColor;
        public ColorF BorderColor;

        public ItemGUIChoices(float x1, float y1, float x2, float y2, float spacing,
            in ColorF backColor, in ColorF borderColor,
            Action backCommand = null)
            : base(x1, y1, spacing, backCommand: backCommand)
        {
            X2 = x2;
            Y2 = y2;
            BackColor = backColor;
            BorderColor = borderColor;
        }

        public override void Render(GL gl)
        {
            uint dstW = GLHelper.CurrentWidth;
            uint dstH = GLHelper.CurrentHeight;
            int x1 = (int)(X * dstW);
            float fy1 = Y * dstH;
            int y1 = (int)fy1;
            int x2 = (int)(X2 * dstW);
            int y2 = (int)(Y2 * dstH);

            // Draw background
            GUIRenderer.Instance.FillRectangle(BackColor, new Rect2D(new Pos2D(x1, y1), new Pos2D(x2, y2))); // TODO: Rounded 10
            GUIRenderer.Instance.DrawRectangle(BorderColor, new Rect2D(new Pos2D(x1, y1), new Pos2D(x2, y2))); // TODO: Rounded 10

            int height = (int)(dstH * Spacing);
            int xOfs = (int)(0.015f * dstW);
            float yOfs = 0.015f * dstH;
            float space = Spacing * dstH;
            int count = _choices.Count;
            for (int i = 0; i < count; i++)
            {
                ItemGUIChoice c = _choices[i];
                bool isSelected = Selected == i;
                int y = (int)(fy1 + (space * i) + yOfs);
                c.Render(gl, isSelected, x1, x2, y, height, xOfs);
            }
        }
    }
}
