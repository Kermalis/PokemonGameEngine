using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
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
            : base(new Vector2(x, y), spacing, backCommand: backCommand)
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

        public override void Render(Vec2I viewSize)
        {
            float tlY = Pos.Y * viewSize.Y;
            int x = (int)(Pos.X * viewSize.X);
            float y = tlY;
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
                Vec2I arrowSize = font.GetSize("→ ");
                // If this is bottom align, we need to adjust the y
                int iy = BottomAligned ? (int)y - arrowSize.Y : (int)y;
                c.Str.Render(new Vec2I(x + arrowSize.X, iy), colors);
                // Draw selection arrow
                if (isSelected)
                {
                    c.ArrowStr.Render(new Vec2I(x, iy), colors);
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
                    y += arrowSize.Y + space;
                }
            }
        }

        public Vec2I GetSize()
        {
            var s = new Vec2I(0, 0);
            float y = 0;
            float space = Spacing;
            int count = _choices.Count;
            for (int i = 0; i < count; i++)
            {
                TextGUIChoice c = _choices[i];
                Font font = c.Str.Font;
                Vec2I textSize = font.GetSize(c.Str.Text);
                Vec2I arrowSize = font.GetSize("→ ");
                int totalWidth = textSize.X + arrowSize.X;
                if (totalWidth > s.X)
                {
                    s.X = totalWidth;
                }
                int totalHeight = textSize.Y + (int)y;
                if (totalHeight > s.Y)
                {
                    s.Y = totalHeight;
                }

                y += textSize.Y + space;
            }
            return s;
        }

        public static void CreateStandardYesNoChoices(Action<bool> clickAction, Vec2I viewSize, out TextGUIChoices choices, out Window window, float x = 0.75f, float y = 0.35f)
        {
            choices = new TextGUIChoices(0f, 0f,
                font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            choices.AddOne("Yes", () => clickAction(true));
            choices.AddOne("No", () => clickAction(false));
            window = Window.CreateFromInnerSize(Vec2I.FromRelative(x, y, viewSize), choices.GetSize(), Colors.White4, Window.Decoration.GrayRounded);
            choices.RenderChoicesOntoWindow(window);
        }
        public void RenderChoicesOntoWindow(Window window)
        {
            window.ClearInner();
            Render(Display.ViewportSize); // Inner size of the window
        }
    }
}
