using Kermalis.PokemonGameEngine.Input;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal class GUIChoice
    {
        public string Text;
        public Action Command;
        public bool IsEnabled;

        public Font Font;
        public uint[] FontColors;
        public uint[] SelectedColors;
        public uint[] DisabledColors;

        public GUIChoice(string text, Action command, bool isEnabled = true,
            Font font = null, uint[] fontColors = null, uint[] selectedColors = null, uint[] disabledColors = null)
        {
            Text = text;
            Command = command;
            IsEnabled = isEnabled;

            Font = font;
            FontColors = fontColors;
            SelectedColors = selectedColors;
            DisabledColors = disabledColors;
        }
    }

    internal class GUIChoices : IEnumerable<GUIChoice>, IDisposable
    {
        private readonly List<GUIChoice> _choices = new List<GUIChoice>();

        public float X;
        public float Y;
        public float Spacing;

        public Action BackCommand;
        public Font Font;
        public uint[] FontColors;
        public uint[] SelectedColors;
        public uint[] DisabledColors;

        public int Selected = 0;

        public GUIChoices(float x, float y, float spacing, Action backCommand = null,
            Font font = null, uint[] fontColors = null, uint[] selectedColors = null, uint[] disabledColors = null)
        {
            X = x;
            Y = y;
            Spacing = spacing;

            BackCommand = backCommand;
            Font = font;
            FontColors = fontColors;
            SelectedColors = selectedColors;
            DisabledColors = disabledColors;
        }

        public void HandleInputs()
        {
            bool down = InputManager.IsPressed(Key.Down);
            bool up = InputManager.IsPressed(Key.Up);
            bool a = InputManager.IsPressed(Key.A);
            bool b = InputManager.IsPressed(Key.B);
            if (!down && !up && !a && !b)
            {
                return;
            }

            if (b)
            {
                Action c = BackCommand;
                if (c != null)
                {
                    c.Invoke();
                    return;
                }
            }

            int curSelected = Selected;
            if (down && curSelected < _choices.Count - 1)
            {
                Selected = curSelected + 1;
            }
            if (up && curSelected > 0)
            {
                Selected = curSelected - 1;
            }
            if (a)
            {
                GUIChoice c = _choices[curSelected];
                if (c.IsEnabled)
                {
                    c.Command.Invoke();
                }
            }
        }

        public unsafe void Render(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            bool drawUpwards = true;
            int count = _choices.Count;
            for (int i = 0; i < count; i++)
            {
                GUIChoice c = _choices[i];
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
                int placementY = drawUpwards ? count - 1 - i : i;
                int y = (int)((bmpHeight * Y) - (bmpHeight * (placementY * Spacing)));
                font.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * X), y, c.Text, colors);
                // Draw selection arrow
                if (isSelected)
                {
                    font.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * (X - 0.05f)), y, "→", colors);
                }
            }
        }

        public void Add(GUIChoice button)
        {
            _choices.Add(button);
        }

        public IEnumerator<GUIChoice> GetEnumerator()
        {
            return _choices.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _choices.GetEnumerator();
        }

        public void Dispose()
        {
            foreach (GUIChoice c in _choices)
            {
                c.Command = null;
            }
        }
    }
}
