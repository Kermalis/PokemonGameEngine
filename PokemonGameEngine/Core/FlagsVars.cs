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
    internal sealed class Vars
    {
        private readonly short[] _values;

        public short this[Var var]
        {
            get => _values[(int)var];
            set => _values[(int)var] = value;
        }

        public Vars()
        {
            _values = new short[(int)Var.MAX];
        }

        public short GetVarOrValue(uint value)
        {
            return value <= ushort.MaxValue ? (short)value : _values[value - (ushort.MaxValue + 1)];
        }
    }
}
