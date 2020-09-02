using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.World
{
    internal interface IXYElevation
    {
        int X { get; }
        int Y { get; }
        byte Elevation { get; }
    }

    internal static class WorldUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasElevation(this byte elevations, byte elevation)
        {
            return (elevations & (1 << elevation)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetLowestElevation(this byte elevations)
        {
            for (byte e = 0; e < Overworld.NumElevations; e++)
            {
                if (elevations.HasElevation(e))
                {
                    return e;
                }
            }
            return 0;
        }

        public static bool IsSamePosition(this IXYElevation this1, IXYElevation other)
        {
            return this1.X == other.X && this1.Y == other.Y && this1.Elevation == other.Elevation;
        }
    }
}
