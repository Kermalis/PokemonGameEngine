using Kermalis.EndianBinaryIO;

namespace Kermalis.PokemonGameEngine.World.Maps
{
    public sealed class MapConnection
    {
        public enum Direction : byte
        {
            South,
            North,
            West,
            East
        }
        public readonly Direction Dir;
        public readonly int MapId;
        public readonly int Offset;

        public MapConnection(EndianBinaryReader r)
        {
            Dir = r.ReadEnum<Direction>();
            MapId = r.ReadInt32();
            Offset = r.ReadInt32();
        }
    }
}
