using Kermalis.EndianBinaryIO;

namespace Kermalis.PokemonGameEngine.World.Maps
{
    internal sealed partial class Map
    {
        public sealed class Connection
        {
            public enum Direction : byte
            {
                South,
                North,
                West,
                East
            }
            public readonly Direction Dir;
            public readonly Map Map;
            public readonly int Offset;

            public Connection(EndianBinaryReader r)
            {
                Dir = r.ReadEnum<Direction>();
                Map = LoadOrGet(r.ReadInt32());
                Map.AddReference();
                Offset = r.ReadInt32();
            }
        }
    }
}
