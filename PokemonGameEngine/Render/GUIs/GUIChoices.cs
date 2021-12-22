using Kermalis.PokemonGameEngine.Input;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    internal abstract class GUIChoice : IDisposable
    {
        public Action Command;
        public bool IsEnabled;
        public virtual bool IsSelected { get; set; }

        public GUIChoice(Action command, bool isEnabled)
        {
            Command = command;
            IsEnabled = isEnabled;
        }

        public virtual void Dispose()
        {
            Command = null;
        }
    }

    internal abstract class GUIChoices<T> : IEnumerable<T>, IDisposable where T : GUIChoice
    {
        protected readonly List<T> _choices = new();

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
            bool down = InputManager.JustPressed(Key.Down);
            bool up = InputManager.JustPressed(Key.Up);
            bool a = InputManager.JustPressed(Key.A);
            bool b = InputManager.JustPressed(Key.B);
            if (!down && !up && !a && !b)
            {
                return;
            }

            if (b)
            {
                Action c = BackCommand;
                if (c is not null)
                {
                    c();
                    return;
                }
            }

            int curSelected = Selected;
            if (down && curSelected < _choices.Count - 1)
            {
                _choices[curSelected].IsSelected = false;
                Selected = curSelected + 1;
                _choices[Selected].IsSelected = true;
            }
            if (up && curSelected > 0)
            {
                _choices[curSelected].IsSelected = false;
                Selected = curSelected - 1;
                _choices[Selected].IsSelected = true;
            }
            if (a)
            {
                T c = _choices[curSelected];
                if (c.IsEnabled)
                {
                    c.Command();
                }
            }
        }

        public abstract void Render();

        public virtual void Add(T c)
        {
            _choices.Add(c);
            if (_choices.Count - 1 == Selected)
            {
                c.IsSelected = true;
            }
        }
        public virtual void Remove(T c)
        {
            _choices.Remove(c);
            if (Selected >= _choices.Count)
            {
                Selected = _choices.Count - 1;
            }
            _choices[Selected].IsSelected = true;
        }
        public virtual void Clear()
        {
            _choices.Clear();
            Selected = 0;
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
                c.Dispose();
            }
        }
    }
}
