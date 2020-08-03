using System.Collections;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class Flags
    {
        private readonly BitArray _bits;

        public bool this[Flag flag]
        {
            get => _bits[(int)flag];
            set => _bits[(int)flag] = value;
        }

        public Flags()
        {
            _bits = new BitArray((int)Flag.MAX);
        }
    }
}
