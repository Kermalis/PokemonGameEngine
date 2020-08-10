using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.World
{
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
    }
}
