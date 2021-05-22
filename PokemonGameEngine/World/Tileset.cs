using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World
{
    internal sealed class Tileset
    {
        public sealed class Tile
        {
            // Hold reference to parent (so animations work) because parent will be unloaded without the reference.
            // The reference to the tile comes from blockset tiles
            public readonly Tileset Parent;
            public readonly uint[] Bitmap;
            public uint[] AnimBitmap;

            public Tile(Tileset parent, uint[] bitmap)
            {
                Parent = parent;
                Bitmap = bitmap;
            }
        }

        public readonly Tile[] Tiles;
        private readonly TileAnimationLive[] _animations;

        private Tileset(string name, int id)
        {
            uint[][] t = RenderUtils.LoadBitmapSheet(TilesetPath + name + TilesetExtension, Overworld.Tile_NumPixelsX, Overworld.Tile_NumPixelsY);
            Tiles = new Tile[t.Length];
            for (int i = 0; i < t.Length; i++)
            {
                Tiles[i] = new Tile(this, t[i]);
            }
            TileAnimation[] a = TileAnimation.LoadOrGet(id);
            if (a != null)
            {
                _animations = new TileAnimationLive[a.Length];
                for (int i = 0; i < a.Length; i++)
                {
                    _animations[i] = new TileAnimationLive(this, a[i]);
                }
            }
        }

        private const string TilesetExtension = ".png";
        private const string TilesetPath = "Tileset.";
        private static readonly IdList _ids = new(TilesetPath + "TilesetIds.txt");
        private static readonly Dictionary<int, WeakReference<Tileset>> _loadedTilesets = new();
        public static Tileset LoadOrGet(int id)
        {
            string name = _ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            Tileset t;
            if (!_loadedTilesets.TryGetValue(id, out WeakReference<Tileset> w))
            {
                t = new Tileset(name, id);
                _loadedTilesets.Add(id, new WeakReference<Tileset>(t));
            }
            else if (!w.TryGetTarget(out t))
            {
                t = new Tileset(name, id);
                w.SetTarget(t);
            }
            return t;
        }

        public static void AnimationTick()
        {
            foreach (WeakReference<Tileset> w in _loadedTilesets.Values)
            {
                if (!w.TryGetTarget(out Tileset t) || t._animations == null)
                {
                    continue;
                }
                foreach (TileAnimationLive a in t._animations)
                {
                    a.Tick();
                }
            }
        }
    }

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
            TileAnimation anim = _anim;
            int c = (_counter + 1) % anim.Duration;
            _counter = c;
            for (int f = 0; f < anim.Frames.Length; f++)
            {
                TileAnimation.Frame frame = anim.Frames[f];
                Tileset.Tile tile = _tileset.Tiles[frame.TilesetTile];
                for (int s = frame.Stops.Length - 1; s >= 0; s--)
                {
                    TileAnimation.Frame.Stop stop = frame.Stops[s];
                    if (stop.Time <= c)
                    {
                        tile.AnimBitmap = anim.Sheet[stop.SheetTile];
                        goto bottom;
                    }
                }
                tile.AnimBitmap = null;
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
                public readonly int SheetTile;
                public readonly int Time;

                public Stop(EndianBinaryReader r)
                {
                    SheetTile = r.ReadInt32();
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
        public readonly uint[][] Sheet;
        public readonly int Duration;
        public readonly Frame[] Frames;

        public TileAnimation(EndianBinaryReader r)
        {
            Sheet = RenderUtils.LoadBitmapSheet(AnimationsPath + r.ReadStringNullTerminated() + AnimationSheetExtension, Overworld.Tile_NumPixelsX, Overworld.Tile_NumPixelsY);
            Duration = r.ReadInt32();
            byte numFrames = r.ReadByte();
            Frames = new Frame[numFrames];
            for (int i = 0; i < numFrames; i++)
            {
                Frames[i] = new Frame(r);
            }
        }

        private static readonly Dictionary<int, uint[]> _animationOffsets;

        private const string AnimationSheetExtension = ".png";
        private const string AnimationsExtension = ".bin";
        private const string AnimationsPath = "Tileset.Animation.";
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
            return new EndianBinaryReader(Utils.GetResourceStream(AnimationsFile), encoding: EncodingType.UTF16);
        }

        private static readonly Dictionary<int, WeakReference<TileAnimation[]>> _loadedAnimations = new();
        public static TileAnimation[] LoadOrGet(int tilesetId)
        {
            if (!_animationOffsets.TryGetValue(tilesetId, out uint[] offsets))
            {
                return null;
            }
            TileAnimation[] Create()
            {
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
            TileAnimation[] a;
            if (!_loadedAnimations.TryGetValue(tilesetId, out WeakReference<TileAnimation[]> w))
            {
                a = Create();
                _loadedAnimations.Add(tilesetId, new WeakReference<TileAnimation[]>(a));
            }
            else if (!w.TryGetTarget(out a))
            {
                a = Create();
                w.SetTarget(a);
            }
            return a;
        }
    }
}
