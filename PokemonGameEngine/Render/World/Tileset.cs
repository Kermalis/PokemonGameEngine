using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.World;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.World
{
    // TODO: Texture atlas
    internal sealed class Tileset
    {
        public sealed class Tile
        {
            // Hold reference to parent (so animations work) because parent will be unloaded without the reference.
            // The reference to the tile comes from blockset tiles
            public readonly Tileset Parent;
            public readonly uint Bitmap;
            public uint AnimBitmap;

            public unsafe Tile(Tileset parent, uint[] bitmap)
            {
                Parent = parent;
                GL gl = Game.OpenGL;
                GLHelper.ActiveTexture(gl, TextureUnit.Texture0);
                uint tex = GLHelper.GenTexture(gl);
                GLHelper.BindTexture(gl, tex);
                fixed (uint* d = bitmap)
                {
                    GLTextureUtils.LoadTextureData(gl, d, new Size2D(Overworld.Tile_NumPixelsX, Overworld.Tile_NumPixelsY));
                }
                Bitmap = tex;
            }
        }

        public readonly Tile[] Tiles;
        private readonly TileAnimationLive[] _animations;

        private Tileset(string name, int id)
        {
            uint[][] t = AssetLoader.GetAssetSheetAsBitmaps(TilesetPath + name + TilesetExtension, new Size2D(Overworld.Tile_NumPixelsX, Overworld.Tile_NumPixelsY));
            Tiles = new Tile[t.Length];
            for (int i = 0; i < t.Length; i++)
            {
                Tiles[i] = new Tile(this, t[i]);
            }
            TileAnimation[] a = TileAnimation.LoadOrGet(id);
            if (a is not null)
            {
                _animations = new TileAnimationLive[a.Length];
                for (int i = 0; i < a.Length; i++)
                {
                    _animations[i] = new TileAnimationLive(this, a[i]);
                }
            }
        }
        // Delete textures when the tileset is unloaded
        ~Tileset()
        {
            Game.AddTempTask(() =>
            {
                GL gl = Game.OpenGL;
                for (int i = 0; i < Tiles.Length; i++)
                {
                    gl.DeleteTexture(Tiles[i].Bitmap);
                }
            });
        }

        public static void AnimationTick()
        {
            foreach (WeakReference<Tileset> w in _loadedTilesets.Values)
            {
                if (!w.TryGetTarget(out Tileset t) || t._animations is null)
                {
                    continue;
                }
                foreach (TileAnimationLive a in t._animations)
                {
                    a.Tick();
                }
            }
        }

        private const string TilesetExtension = ".png";
        private const string TilesetPath = "Tileset\\";
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
    }
}
