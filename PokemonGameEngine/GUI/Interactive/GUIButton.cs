using System.Collections;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI.Interactive
{
    internal abstract class GUIButton
    {
        public bool IsEnabled { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }

    internal class GUIButtons<T> : IEnumerable<T> where T : GUIButton
    {
        private readonly List<T> _buttons = new();

        public void Add(T button)
        {
            _buttons.Add(button);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _buttons.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _buttons.GetEnumerator();
        }
    }
}
