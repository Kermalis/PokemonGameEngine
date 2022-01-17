using Kermalis.EndianBinaryIO;

namespace Kermalis.PokemonGameEngine.World.Data
{
    internal readonly struct TileAnimationData
    {
        public readonly struct Frame
        {
            public readonly struct Stop
            {
                public readonly int AnimTile;
                public readonly float Time;

                public Stop(EndianBinaryReader r)
                {
                    AnimTile = r.ReadInt32();
                    Time = r.ReadSingle();
                }
            }

            public readonly int TilesetTile;
            public readonly Stop[] Stops;

            public Frame(EndianBinaryReader r)
            {
                TilesetTile = r.ReadInt32();
                byte numStops = r.ReadByte();
                Stops = new Stop[numStops];
                for (int i = 0; i < numStops; i++)
                {
                    Stops[i] = new Stop(r);
                }
            }
        }

        public readonly float Duration;
        public readonly Frame[] Frames;

        public TileAnimationData(EndianBinaryReader r)
        {
            Duration = r.ReadSingle();
            byte numFrames = r.ReadByte();
            Frames = new Frame[numFrames];
            for (int i = 0; i < numFrames; i++)
            {
                Frames[i] = new Frame(r);
            }
        }
    }
}
