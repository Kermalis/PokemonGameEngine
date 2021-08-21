using Kermalis.PokemonGameEngine.World.Maps;

namespace Kermalis.PokemonGameEngine.World
{
    internal readonly struct Warp
    {
        public readonly int DestMapId;
        public readonly WorldPos DestPos;

        public Warp(int destMapId, in WorldPos pos)
        {
            DestMapId = destMapId;
            DestPos = pos;
        }
    }
    internal sealed class WarpInProgress
    {
        public static WarpInProgress Current { get; private set; }

        public readonly Warp Destination;
        public readonly Map DestMapLoaded;

        private WarpInProgress(in Warp dest)
        {
            Destination = dest;
            DestMapLoaded = Map.LoadOrGet(dest.DestMapId);
        }
        public static WarpInProgress Start(in Warp dest)
        {
            return Current = new WarpInProgress(dest);
        }
        public static void EndCurrent()
        {
            Current = null;
        }
    }
}
