using Kermalis.EndianBinaryIO;
using System.Collections;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class Flags
    {
        private const int NumBytes = ((int)Flag.MAX / 8) + ((int)Flag.MAX % 8 != 0 ? 1 : 0);
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
            _bits = new BitArray(NumBytes * 8);
        }
        public Flags(EndianBinaryReader r)
        {
            _bits = new BitArray(r.ReadBytes(NumBytes));
        }
        public void Save(EndianBinaryWriter w)
        {
            byte[] buf = new byte[NumBytes];
            _bits.CopyTo(buf, 0);
            w.Write(buf);
        }

        public int GetNumBadges()
        {
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                if (this[(Flag)((int)Flag.Badge1 + i)])
                {
                    count++;
                }
            }
            return count;
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
        public Vars(EndianBinaryReader r)
        {
            _values = r.ReadInt16s((int)Var.MAX);
        }
        public void Save(EndianBinaryWriter w)
        {
            w.Write(_values);
        }

        public short GetVarOrValue(int value)
        {
            // Support both short and ushort
            if ((value is >= short.MinValue and <= short.MaxValue)
                || (value is >= ushort.MinValue and <= ushort.MaxValue))
            {
                return (short)value;
            }
            return _values[value - (ushort.MaxValue + 1)];
        }
    }
}
