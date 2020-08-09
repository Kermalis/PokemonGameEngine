using System.Collections;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class Flags
    {
        private readonly BitArray _bits;

        public bool this[Flag flag]
        {
            get
            {
                if (flag == Flag.MAX)
                {
                    return false;
                }
                else
                {
                    return _bits[(int)flag];
                }
            }
            set
            {
                if (flag != Flag.MAX)
                {
                    _bits[(int)flag] = value;
                }
            }
        }

        public Flags()
        {
            _bits = new BitArray((int)Flag.MAX);
        }
    }
}
