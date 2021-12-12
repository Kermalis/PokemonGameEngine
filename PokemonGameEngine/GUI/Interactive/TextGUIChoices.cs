using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.GUI.Interactive
{
    internal sealed class TextGUIChoice : GUIChoice
    {
        public GUIString ArrowStr;
        public GUIString Str;
        public Vector4[] TextColors;
        public Vector4[] SelectedColors;
        public Vector4[] DisabledColors;

        public TextGUIChoice(GUIString str, Action command, Vector4[] textColors, Vector4[] selectedColors, Vector4[] disabledColors, bool isEnabled)
            : base(command, isEnabled)
        {
            ArrowStr = new GUIString("→", str.Font, null);
            Str = str;
            TextColors = textColors;
            SelectedColors = selectedColors;
            DisabledColors = disabledColors;
        }

        public override void Dispose()
        {
            Command = null;
            ArrowStr.Delete();
            Str.Delete();
        }
    }

    // "Spacing" in this class represents absolute pixels
    internal sealed class TextGUIChoices : GUIChoices<TextGUIChoice>
    {
        public bool BottomAligned;
        public Font Font;
        public Vector4[] TextColors;
        public Vector4[] SelectedColors;
        public Vector4[] DisabledColors;

        public TextGUIChoices(float x, float y, float spacing = 3, bool bottomAlign = false, Action backCommand = null,
            Font font = null, Vector4[] textColors = null, Vector4[] selectedColors = null, Vector4[] disabledColors = null)
            : base(x, y, spacing, backCommand: backCommand)
        {
            Font = font;
            TextColors = textColors;
            SelectedColors = selectedColors;
            DisabledColors = disabledColors;
            BottomAligned = bottomAlign;
        }

        public void AddOne(string text, Action command, bool isEnabled = true,
            Font font = null, Vector4[] textColors = null, Vector4[] selectedColors = null, Vector4[] disabledColors = null)
        {
            font ??= Font;
            textColors ??= TextColors;
            selectedColors ??= SelectedColors;
            disabledColors ??= DisabledColors;
            var str = new GUIString(text, font, null);
            Add(new TextGUIChoice(str, command, textColors, selectedColors, disabledColors, isEnabled));
        }

        public override void Render()
        {
            float y1 = Y * GLHelper.CurrentHeight;
            int x = Renderer.RelXToAbsX(X);
            float y = y1;
            float space = Spacing;
            int count = _choices.Count;
            int i = BottomAligned ? count - 1 : 0;
            while (true)
            {
                TextGUIChoice c = _choices[i];
                Font font = c.Str.Font;
                bool isSelected = Selected == i;
                Vector4[] colors;
                if (c.IsEnabled)
                {
                    colors = isSelected ? c.SelectedColors : c.TextColors;
                }
                else
                {
                    colors = c.DisabledColors;
                }
                Size2D arrowSize = font.MeasureString("→ ");
                // If this is bottom align, we need to adjust the y
                int iy = BottomAligned ? (int)y - (int)arrowSize.Height : (int)y;
                c.Str.Render(new Pos2D(x + (int)arrowSize.Width, iy), colors);
                // Draw selection arrow
                if (isSelected)
                {
                    c.ArrowStr.Render(new Pos2D(x, iy), colors);
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
                    y += arrowSize.Height + space;
                }
            }
        }

        public Size2D GetSize()
        {
            var s = new Size2D(0, 0);
            float y = 0;
            float space = Spacing;
            int count = _choices.Count;
            for (int i = 0; i < count; i++)
            {
                TextGUIChoice c = _choices[i];
                Font font = c.Str.Font;
                Size2D textSize = font.MeasureString(c.Str.Text);
                Size2D arrowSize = font.MeasureString("→ ");
                uint totalWidth = textSize.Width + arrowSize.Width;
                if (totalWidth > s.Width)
                {
                    s.Width = totalWidth;
                }
                uint totalHeight = textSize.Height + (uint)y;
                if (totalHeight > s.Height)
                {
                    s.Height = totalHeight;
                }

                y += textSize.Height + space;
            }
            return s;
        }

        public static void CreateStandardYesNoChoices(Action<bool> clickAction, out TextGUIChoices choices, out Window window, float x = 0.8f, float y = 0.4f)
        {
            choices = new TextGUIChoices(0, 0, font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            choices.AddOne("Yes", () => clickAction(true));
            choices.AddOne("No", () => clickAction(false));
            Size2D s = choices.GetSize();
            window = new Window(new RelPos2D(x, y), s, Colors.White4);
            choices.RenderChoicesOntoWindow(window);
        }
        public void RenderChoicesOntoWindow(Window window)
        {
            GL gl = Display.OpenGL;
            window.Image.PushFrameBuffer(gl);
            window.ClearImagePushed(gl);
            Render();
            GLHelper.PopFrameBuffer(gl);
        }
    }
}
