using Kermalis.PokemonGameEngine.Render;
using System;

namespace Kermalis.PokemonGameEngine.World
{
    internal struct WorldPos
    {
        public Vec2I XY;
        public byte Elevation;

        public WorldPos(Vec2I xy, byte e)
        {
            XY = xy;
            Elevation = e;
        }

        public static bool operator ==(WorldPos a, WorldPos b)
        {
            return a.Elevation == b.Elevation && a.XY == b.XY;
        }
        public static bool operator !=(WorldPos a, WorldPos b)
        {
            return a.Elevation != b.Elevation || a.XY != b.XY;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldPos other && other == this;
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
