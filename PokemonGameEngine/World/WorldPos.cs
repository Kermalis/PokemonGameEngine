using System;

namespace Kermalis.PokemonGameEngine.World
{
    internal struct WorldPos
    {
        public int X;
        public int Y;
        public byte Elevation;

        public WorldPos(int x, int y, byte e)
        {
            X = x;
            Y = y;
            Elevation = e;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldPos other && X == other.X && Y == other.Y && Elevation == other.Elevation;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Elevation);
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("[[X: {0}, Y: {1}], E: {2}]", X, Y, Elevation);
        }
#endif
    }
}
