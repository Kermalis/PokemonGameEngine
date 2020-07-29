namespace Kermalis.PokemonGameEngine.Overworld
{
    internal interface IWarp
    {
        int DestMapId { get; }
        int DestX { get; }
        int DestY { get; }
        byte DestElevation { get; }
    }
    internal sealed class Warp : IWarp
    {
        public int DestMapId { get; }
        public int DestX { get; }
        public int DestY { get; }
        public byte DestElevation { get; }

        public Warp(int destMapId, int destX, int destY, byte destElevation)
        {
            DestMapId = destMapId;
            DestX = destX;
            DestY = destY;
            DestElevation = destElevation;
        }
    }
}
