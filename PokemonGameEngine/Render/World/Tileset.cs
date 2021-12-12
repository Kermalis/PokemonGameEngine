using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Data;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
#if DEBUG_OVERWORLD
using Kermalis.PokemonGameEngine.Debug;
#endif

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed class Tileset
    {
        public sealed class Tile
        {
            public readonly int Id;
            public int AnimId = TileAnimation.NO_ANIM_ID;

            public Tile(int id)
            {
                Id = id;
            }
        }

        public readonly int Id;

        public readonly Tile[] Tiles;
        public readonly uint Texture;
        public readonly Size2D TextureSize;
        public readonly int NumTilesX;
        private readonly TileAnimation[] _animations;

        private unsafe Tileset(int id, string name)
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Loading tileset: " + name);
#endif

            Id = id;

            // Get texture
            AssetLoader.GetAssetBitmap(TilesetPath + name + TilesetExtension, out TextureSize, out uint[] bitmap);
            NumTilesX = (int)TextureSize.Width / Overworld.Tile_NumPixelsX;

            // Create gl texture
            GL gl = Display.OpenGL;
            gl.ActiveTexture(TextureUnit.Texture0);
            Texture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, Texture);
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
            _animations = TilesetAnimationLoader.Load(this);

            _numReferences = 1;
            _loadedTilesets.Add(id, this);
        }

        public static void UpdateAnimations()
        {
            foreach (Tileset t in _loadedTilesets.Values)
            {
                if (t._animations is not null)
                {
                    foreach (TileAnimation a in t._animations)
                    {
                        a.Update();
                    }
                }
            }
        }

        #region Cache

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
            GL gl = Display.OpenGL;
            gl.DeleteTexture(Texture);
            _loadedTilesets.Remove(Id);
        }

        #endregion
    }
}
