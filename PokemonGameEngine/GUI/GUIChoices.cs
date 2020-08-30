using Kermalis.PokemonGameEngine.Input;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal abstract class GUIChoice
    {
        public Action Command;
        public bool IsEnabled;

        public GUIChoice(Action command, bool isEnabled = true)
        {
            Command = command;
            IsEnabled = isEnabled;
        }
    }

    internal abstract class GUIChoices<T> : IEnumerable<T>, IDisposable where T : GUIChoice
    {
        protected readonly List<T> _choices = new List<T>();

        public float X;
        public float Y;
        public float Spacing;

        public Action BackCommand;

        public int Selected = 0;

        public GUIChoices(float x, float y, float spacing, Action backCommand = null)
        {
            X = x;
            Y = y;
            Spacing = spacing;

            BackCommand = backCommand;
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
                T c = _choices[curSelected];
                if (c.IsEnabled)
                {
                    c.Command.Invoke();
                }
            }
        }

        public abstract unsafe void Render(uint* bmpAddress, int bmpWidth, int bmpHeight);

        public void Add(T button)
        {
            _choices.Add(button);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _choices.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _choices.GetEnumerator();
        }

        public virtual void Dispose()
        {
            foreach (T c in _choices)
            {
                c.Command = null;
            }
        }
    }
}
