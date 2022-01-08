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
        public class Tile
        {
            public readonly Tileset Tileset; // Store here to save memory (there are usually way more Blockset.Block.Tile)
            public readonly int Id;

            public Tile(Tileset t, int id)
            {
                Tileset = t;
                Id = id;
            }
        }
        public sealed class AnimatedTile : Tile
        {
            public int AnimId = TileAnimation.NO_ANIM_ID;
            public bool IsDirty;

            public AnimatedTile(Tileset t, int id)
                : base(t, id)
            {
            }
        }

        private const string TILESET_PATH = @"Tileset\";
        private const string TILESET_EXTENSION = ".png";
        private static readonly IdList _ids = new(TILESET_PATH + "TilesetIds.txt");
        private static readonly Dictionary<int, Tileset> _loadedTilesets = new();

#if DEBUG_OVERWORLD
        public readonly string Name;
#endif
        public readonly int Id;
        private int _numReferences;

        private readonly TileAnimation[] _animations;
        public readonly Tile[] Tiles;
        public readonly uint Texture;
        public readonly Vec2I TextureSize;
        public readonly int NumTilesX;

        private unsafe Tileset(int id, string name)
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Loading tileset: " + name);
            Name = name;
#endif

            Id = id;
            _numReferences = 1;
            _loadedTilesets.Add(id, this);

            // Create gl texture
            GL gl = Display.OpenGL;
            Texture = gl.GenTexture();
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, Texture);
            uint[] bitmap = AssetLoader.GetAssetBitmap(TILESET_PATH + name + TILESET_EXTENSION, out TextureSize);
            fixed (uint* d = bitmap)
            {
                GLTextureUtils.LoadTextureData(gl, d, TextureSize);
            }
            NumTilesX = TextureSize.X / Overworld.Tile_NumPixelsX;

            // Load animations if they exist
            _animations = TilesetAnimationLoader.Load(id);

            // Create tiles
            int numTiles = (TextureSize / Overworld.Tile_NumPixels).GetArea();
            Tiles = new Tile[numTiles];
            for (int i = 0; i < numTiles; i++)
            {
                Tiles[i] = IsAnimated(i) ? new AnimatedTile(this, i) : new Tile(this, i);
            }
        }
        private bool IsAnimated(int tileId)
        {
            if (_animations is not null)
            {
                for (int i = 0; i < _animations.Length; i++)
                {
                    if (_animations[i].ContainsTile(tileId))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void UpdateAnimations()
        {
            foreach (Tileset t in _loadedTilesets.Values)
            {
                if (t._animations is not null)
                {
                    for (int i = 0; i < t._animations.Length; i++)
                    {
                        t._animations[i].Update(t);
                    }
                }
            }
        }
        public static void FinishUpdateAnimations()
        {
            foreach (Tileset t in _loadedTilesets.Values)
            {
                if (t._animations is not null)
                {
                    for (int i = 0; i < t._animations.Length; i++)
                    {
                        t._animations[i].FinishUpdate(t);
                    }
                }
            }
        }

        public static Tileset LoadOrGet(int id)
        {
            if (_loadedTilesets.TryGetValue(id, out Tileset t))
            {
                t._numReferences++;
#if DEBUG_OVERWORLD
                Log.WriteLine("Adding reference to tileset: " + t.Name + " (new count is " + t._numReferences + ")");
#endif
            }
            else
            {
                string name = _ids[id];
                if (name is null)
                {
                    throw new ArgumentOutOfRangeException(nameof(id));
                }
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

#if DEBUG_OVERWORLD
        public override string ToString()
        {
            return Name;
        }
#endif
    }
}
