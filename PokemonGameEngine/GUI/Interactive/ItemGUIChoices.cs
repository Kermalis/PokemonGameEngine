using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Render;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Interactive
{
    internal sealed class ItemGUIChoice : GUIChoice
    {
        private readonly string _itemName;
        private readonly string _quantityStr;
        private readonly int _quantityWidth;

        public ItemGUIChoice(InventorySlot slot, Action command, bool isEnabled = true)
            : base(command, isEnabled: isEnabled)
        {
            _itemName = ItemData.GetItemName(slot.Item);
            _quantityStr = "x" + slot.Quantity.ToString();
            Font.Default.MeasureString(_quantityStr, out _quantityWidth, out _);
        }

        public unsafe void Render(uint* bmpAddress, int bmpWidth, int bmpHeight,
            bool isSelected, int x1, int x2, int y, int height, int xOfs)
        {
            if (isSelected)
            {
                RenderUtils.FillRectangle_Points(bmpAddress, bmpWidth, bmpHeight, x1, y, x2, y + height - 1, RenderUtils.Color(255, 0, 0, 128));
            }
            x1 += xOfs;
            x2 -= xOfs;
            Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, x1, y, _itemName, Font.DefaultDarkGray_I);
            Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, x2 - _quantityWidth, y, _quantityStr, Font.DefaultDarkGray_I);
        }
    }

    internal sealed class ItemGUIChoices : GUIChoices<ItemGUIChoice>
    {
        public float X2;
        public float Y2;

        public uint BackColor;
        public uint BorderColor;

        public ItemGUIChoices(float x1, float y1, float x2, float y2, float spacing,
            uint backColor, uint borderColor,
            Action backCommand = null)
            : base(x1, y1, spacing, backCommand: backCommand)
        {
            X2 = x2;
            Y2 = y2;
            BackColor = backColor;
            BorderColor = borderColor;
        }

        public override unsafe void Render(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            int x1 = (int)(X * bmpWidth);
            float fy1 = Y * bmpHeight;
            int y1 = (int)fy1;
            int x2 = (int)(X2 * bmpWidth);
            int y2 = (int)(Y2 * bmpHeight);

            // Draw background
            RenderUtils.FillRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, x1, y1, x2, y2, 10, BackColor);
            RenderUtils.DrawRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, x1, y1, x2, y2, 10, BorderColor);

            int height = (int)(bmpHeight * Spacing);
            int xOfs = (int)(0.015f * bmpWidth);
            float yOfs = 0.015f * bmpHeight;
            float space = Spacing * bmpHeight;
            int count = _choices.Count;
            for (int i = 0; i < count; i++)
            {
                ItemGUIChoice c = _choices[i];
                bool isSelected = Selected == i;
                int y = (int)(fy1 + (space * i) + yOfs);
                c.Render(bmpAddress, bmpWidth, bmpHeight, isSelected, x1, x2, y, height, xOfs);
            }
        }
    }
}
