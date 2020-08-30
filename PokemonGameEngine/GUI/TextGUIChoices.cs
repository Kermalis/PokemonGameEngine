using System;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal class TextGUIChoice : GUIChoice
    {
        public string Text;

        public Font Font;
        public uint[] FontColors;
        public uint[] SelectedColors;
        public uint[] DisabledColors;

        public TextGUIChoice(string text, Action command, bool isEnabled = true,
            Font font = null, uint[] fontColors = null, uint[] selectedColors = null, uint[] disabledColors = null)
            : base(command, isEnabled: isEnabled)
        {
            Text = text;

            Font = font;
            FontColors = fontColors;
            SelectedColors = selectedColors;
            DisabledColors = disabledColors;
        }
    }

    internal class TextGUIChoices : GUIChoices<TextGUIChoice>
    {
        public Font Font;
        public uint[] FontColors;
        public uint[] SelectedColors;
        public uint[] DisabledColors;

        public TextGUIChoices(float x, float y, float spacing, Action backCommand = null,
            Font font = null, uint[] fontColors = null, uint[] selectedColors = null, uint[] disabledColors = null)
            : base(x, y, spacing, backCommand: backCommand)
        {
            Font = font;
            FontColors = fontColors;
            SelectedColors = selectedColors;
            DisabledColors = disabledColors;
        }

        public override unsafe void Render(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            float y1 = Y * bmpHeight;
            float space = bmpHeight * Spacing;
            bool bottomToTop = true;
            int count = _choices.Count;
            for (int i = 0; i < count; i++)
            {
                TextGUIChoice c = _choices[i];
                Font font = c.Font ?? Font;
                bool isSelected = Selected == i;
                uint[] colors;
                if (c.IsEnabled)
                {
                    colors = isSelected
                        ? c.SelectedColors ?? SelectedColors
                        : c.FontColors ?? FontColors;
                }
                else
                {
                    colors = c.DisabledColors ?? DisabledColors;
                }
                int y;
                if (bottomToTop)
                {
                    y = (int)(y1 - (space * (count - 1 - i)));
                }
                else
                {
                    y = (int)(y1 + (space * i));
                }
                font.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * X), y, c.Text, colors);
                // Draw selection arrow
                if (isSelected)
                {
                    font.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * (X - 0.05f)), y, "→", colors);
                }
            }
        }
    }
}
