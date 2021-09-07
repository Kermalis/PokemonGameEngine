#if DEBUG_OVERWORLD
using Kermalis.PokemonGameEngine.Debug;
#endif
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.World;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed class Tileset
    {
        public sealed class Tile
        {
            public const int NoAnim = -1;
            public const int InvalidAnim = -2;

            public readonly int Id;
            public int AnimId = NoAnim;

            public Tile(int id)
            {
                Id = id;
            }
        }

        public readonly Tile[] Tiles;
        public readonly uint Texture;
        public readonly Size2D TextureSize;
        public readonly int NumTilesX;
        private readonly TileAnimationLive[] _animations;

        private unsafe Tileset(int id, string name)
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Loading tileset: " + name);
#endif

            // Get texture
            AssetLoader.GetAssetBitmap(TilesetPath + name + TilesetExtension, out TextureSize, out uint[] bitmap);
            NumTilesX = (int)TextureSize.Width / Overworld.Tile_NumPixelsX;

            // Create gl texture
            GL gl = Game.OpenGL;
            GLHelper.ActiveTexture(gl, TextureUnit.Texture0);
            Texture = GLHelper.GenTexture(gl);
            GLHelper.BindTexture(gl, Texture);
            fixed (uint* d = bitmap)
            {
                GLTextureUtils.LoadTextureData(gl, d, TextureSize);
            }

            // Create tiles
            uint numTiles = TextureSize.Width / Overworld.Tile_NumPixelsX * (TextureSize.Height / Overworld.Tile_NumPixelsY);
            Tiles = new Tile[numTiles];
            for (int i = 0; i < numTiles; i++)
            {
                Tiles[i] = new Tile(i);
            }

            // Load animations if they exist
            TileAnimation[] a = TileAnimation.Load(id);
            if (a is not null)
            {
                _animations = new TileAnimationLive[a.Length];
                for (int i = 0; i < a.Length; i++)
                {
                    _animations[i] = new TileAnimationLive(this, a[i]);
                }
            }

            Id = id;
            _numReferences = 1;
            _loadedTilesets.Add(id, this);
        }

        public static void AnimationTick()
        {
            foreach (Tileset t in _loadedTilesets.Values)
            {
                if (t._animations is not null)
                {
                    foreach (TileAnimationLive a in t._animations)
                    {
                        a.Tick();
                    }
                }
            }
        }

        #region Cache

        public readonly int Id;
        private int _numReferences;

        private const string TilesetExtension = ".png";
        private const string TilesetPath = "Tileset\\";
        private static readonly IdList _ids = new(TilesetPath + "TilesetIds.txt");
        private static readonly Dictionary<int, Tileset> _loadedTilesets = new();
        public static Tileset LoadOrGet(int id)
        {
            string name = _ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            if (_loadedTilesets.TryGetValue(id, out Tileset t))
            {
                t._numReferences++;
#if DEBUG_OVERWORLD
                Log.WriteLine("Adding reference to tileset: " + name + " (new count is " + t._numReferences + ")");
#endif
            }
            else
            {
                t = new Tileset(id, name);
            }
            return t;
        }

        public void DeductReference()
        {
            if (--_numReferences > 0)
            {
#if DEBUG_OVERWORLD
                Log.WriteLine("Removing reference from tileset: " + _ids[Id] + " (new count is " + _numReferences + ")");
#endif
                return;
            }

#if DEBUG_OVERWORLD
            Log.WriteLine("Unloading tileset: " + _ids[Id]);
#endif
            GL gl = Game.OpenGL;
            gl.DeleteTexture(Texture);
            _loadedTilesets.Remove(Id);
        }

        #endregion
    }
}
