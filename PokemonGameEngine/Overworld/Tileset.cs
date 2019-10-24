using Kermalis.PokemonGameEngine.Util;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Overworld
{
    internal sealed class Tileset
    {
        public sealed class Tile
        {
            public readonly Tileset Parent;
            public readonly uint[][] Colors;

            public Tile(Tileset parent, uint[][] colors)
            {
                Parent = parent;
                Colors = colors;
            }
        }

        public readonly Tile[] Tiles;

        private Tileset(string name)
        {
            uint[][][] t = RenderUtil.LoadSpriteSheet("Tileset." + name + ".png", 8, 8);
            Tiles = new Tile[t.Length];
            for (int i = 0; i < t.Length; i++)
            {
                Tiles[i] = new Tile(this, t[i]);
            }
        }

        private static readonly IdList _ids = new IdList("Tileset.TilesetIds.txt");
        private static readonly List<WeakReference<Tileset>> _loadedTilesets = new List<WeakReference<Tileset>>();
        public static Tileset LoadOrGet(int id)
        {
            string name = _ids[id];
            if (name == null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            Tileset t;
            if (id >= _loadedTilesets.Count)
            {
                t = new Tileset(name);
                _loadedTilesets.Add(new WeakReference<Tileset>(t));
                return t;
            }
            if (_loadedTilesets[id].TryGetTarget(out t))
            {
                return t;
            }
            t = new Tileset(name);
            _loadedTilesets[id].SetTarget(t);
            return t;
        }
    }
}
