using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.World;
using Silk.NET.OpenGL;
using System;
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
                        tile.AnimBitmap = anim.Textures[stop.SheetTile];
                        goto bottom;
                    }
                }
                tile.AnimBitmap = 0;
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
        public readonly uint[] Textures;
        public readonly int Duration;
        public readonly Frame[] Frames;

        public unsafe TileAnimation(EndianBinaryReader r)
        {
            uint[][] sheet = Renderer.GetResourceSheetAsBitmaps(AnimationsPath + r.ReadStringNullTerminated() + AnimationSheetExtension, new Size2D(Overworld.Tile_NumPixelsX, Overworld.Tile_NumPixelsY));
            Textures = new uint[sheet.Length];
            GL gl = Game.OpenGL;
            GLHelper.ActiveTexture(gl, TextureUnit.Texture0);
            for (int i = 0; i < sheet.Length; i++)
            {
                uint tex = GLHelper.GenTexture(gl);
                GLHelper.BindTexture(gl, tex);
                fixed (uint* d = sheet[i])
                {
                    GLTextureUtils.LoadTextureData(gl, d, Overworld.Tile_NumPixelsX, Overworld.Tile_NumPixelsY);
                }
                Textures[i] = tex;
            }
            Duration = r.ReadInt32();
            byte numFrames = r.ReadByte();
            Frames = new Frame[numFrames];
            for (int i = 0; i < numFrames; i++)
            {
                Frames[i] = new Frame(r);
            }
        }
        // Delete textures when the animation is unloaded
        ~TileAnimation()
        {
            Game.AddTempTask(() =>
            {
                GL gl = Game.OpenGL;
                for (int i = 0; i < Textures.Length; i++)
                {
                    gl.DeleteTexture(Textures[i]);
                }
            });
        }

        #region Loading

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

        #endregion

        #region Cache

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

        #endregion
    }
}
