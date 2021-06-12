using Kermalis.PokemonGameEngine.Render;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Interactive
{
    internal sealed class TextGUIChoice : GUIChoice
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

    // "Spacing" in this class represents absolute pixels
    internal sealed class TextGUIChoices : GUIChoices<TextGUIChoice>
    {
        public bool BottomAligned;
        public Font Font;
        public uint[] FontColors;
        public uint[] SelectedColors;
        public uint[] DisabledColors;

        public TextGUIChoices(float x, float y, float spacing = 3, bool bottomAlign = false, Action backCommand = null,
            Font font = null, uint[] fontColors = null, uint[] selectedColors = null, uint[] disabledColors = null)
            : base(x, y, spacing, backCommand: backCommand)
        {
            Font = font;
            FontColors = fontColors;
            SelectedColors = selectedColors;
            DisabledColors = disabledColors;
            BottomAligned = bottomAlign;
        }

        public override unsafe void Render(uint* dst, int dstW, int dstH)
        {
            float y1 = Y * dstH;
            float y = y1;
            float space = Spacing;
            int count = _choices.Count;
            int i = BottomAligned ? count - 1 : 0;
            while (true)
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
                font.MeasureString("→ ", out int arrowW, out int textH);
                int x = (int)(dstW * X);
                // If this is bottom align, we need to adjust the y
                int iy = BottomAligned ? (int)y - textH : (int)y;
                font.DrawString(dst, dstW, dstH, x + arrowW, iy, c.Text, colors);
                // Draw selection arrow
                if (isSelected)
                {
                    font.DrawString(dst, dstW, dstH, x, iy, "→", colors);
                }

                if (BottomAligned)
                {
                    if (--i < 0)
                    {
                        break;
                    }
                    y = iy - space;
                }
                else
                {
                    if (++i >= count)
                    {
                        break;
                    }
                    y += textH + space;
                }
            }
        }

        public void GetSize(out int width, out int height)
        {
            width = 0;
            height = 0;
            float y = 0;
            float space = Spacing;
            int count = _choices.Count;
            for (int i = 0; i < count; i++)
            {
                TextGUIChoice c = _choices[i];
                Font font = c.Font ?? Font;
                font.MeasureString(c.Text, out int textW, out int textH);
                font.MeasureString("→ ", out int arrowW, out _);
                int totalWidth = textW + arrowW;
                if (totalWidth > width)
                {
                    width = totalWidth;
                }
                int totalHeight = textH + (int)y;
                if (totalHeight > height)
                {
                    height = totalHeight;
                }

                y += textH + space;
            }
        }

        public static void CreateStandardYesNoChoices(Action<bool> clickAction, out TextGUIChoices choices, out Window window, float x = 0.8f, float y = 0.4f)
        {
            choices = new TextGUIChoices(0, 0, font: Font.Default, fontColors: Font.DefaultDarkGray_I, selectedColors: Font.DefaultYellow_O);
            choices.Add(new TextGUIChoice("Yes", () => clickAction(true)));
            choices.Add(new TextGUIChoice("No", () => clickAction(false)));
            choices.GetSize(out int width, out int height);
            window = new Window(x, y, width, height, Renderer.Color(255, 255, 255, 255));
            choices.RenderChoicesOntoWindow(window);
        }
        public unsafe void RenderChoicesOntoWindow(Window window)
        {
            window.ClearImage();
            Image i = window.Image;
            fixed (uint* dst = i.Bitmap)
            {
                Render(dst, i.Width, i.Height);
            }
        }
    }
}
