using Kermalis.PokemonGameEngine.Render;
using System;

namespace Kermalis.PokemonGameEngine.World
{
    internal struct WorldPos
    {
        public Pos2D XY;
        public byte Elevation;

        public WorldPos(Pos2D xy, byte e)
        {
            XY = xy;
            Elevation = e;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldPos other && XY.Equals(other.XY) && Elevation == other.Elevation;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(XY, Elevation);
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("[{0}, E: {1}]", XY, Elevation);
        }
#endif
    }
}
