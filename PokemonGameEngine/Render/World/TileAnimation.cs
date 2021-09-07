using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.World;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed class TileAnimationLive
    {
        private readonly Tileset _tileset;
        private readonly TileAnimation _anim;

        private int _counter;

        public TileAnimationLive(Tileset tileset, TileAnimation anim)
        {
            _tileset = tileset;
            _anim = anim;
        }

        public void Tick()
        {
            int c = (_counter + 1) % _anim.Duration;
            _counter = c;
            for (int f = 0; f < _anim.Frames.Length; f++)
            {
                TileAnimation.Frame frame = _anim.Frames[f];
                Tileset.Tile tile = _tileset.Tiles[frame.TilesetTile];
                for (int s = frame.Stops.Length - 1; s >= 0; s--)
                {
                    TileAnimation.Frame.Stop stop = frame.Stops[s];
                    if (stop.Time <= c)
                    {
                        tile.AnimId = stop.AnimTile;
                        goto bottom;
                    }
                }
                tile.AnimId = Tileset.Tile.NoAnim;
            bottom:
                ;
            }
        }
    }

    internal sealed class TileAnimation
    {
        public sealed class Frame
        {
            public sealed class Stop
            {
                public readonly int AnimTile;
                public readonly int Time;

                public Stop(EndianBinaryReader r)
                {
                    AnimTile = r.ReadInt32();
                    Time = r.ReadInt32();
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
        public readonly int Duration;
        public readonly Frame[] Frames;

        private TileAnimation(EndianBinaryReader r)
        {
            Duration = r.ReadInt32();
            byte numFrames = r.ReadByte();
            Frames = new Frame[numFrames];
            for (int i = 0; i < numFrames; i++)
            {
                Frames[i] = new Frame(r);
            }
        }

        #region Loading

        private static readonly Dictionary<int, uint[]> _animationOffsets;

        private const string AnimationsExtension = ".bin";
        private const string AnimationsPath = "Tileset\\Animation\\";
        private const string AnimationsFile = AnimationsPath + "Animations" + AnimationsExtension;
        static TileAnimation()
        {
            using (EndianBinaryReader r = GetReader())
            {
                int numTilesets = r.ReadInt32();
                _animationOffsets = new Dictionary<int, uint[]>(numTilesets);
                for (int i = 0; i < numTilesets; i++)
                {
                    int tilesetId = r.ReadInt32();
                    int numAnims = r.ReadInt32();
                    uint[] anims = r.ReadUInt32s(numAnims);
                    _animationOffsets.Add(tilesetId, anims);
                }
            }
        }

        private static EndianBinaryReader GetReader()
        {
            return new EndianBinaryReader(AssetLoader.GetAssetStream(AnimationsFile), encoding: EncodingType.UTF16);
        }

        public static TileAnimation[] Load(int tilesetId)
        {
            if (!_animationOffsets.TryGetValue(tilesetId, out uint[] offsets))
            {
                return null;
            }
            using (EndianBinaryReader r = GetReader())
            {
                var arr = new TileAnimation[offsets.Length];
                for (int i = 0; i < offsets.Length; i++)
                {
                    r.BaseStream.Position = offsets[i];
                    arr[i] = new TileAnimation(r);
                }
                return arr;
            }
        }

        #endregion
    }
}
