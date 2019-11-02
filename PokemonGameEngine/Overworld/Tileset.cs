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
            uint[][][] t = RenderUtil.LoadSpriteSheet(_tilesetPath + name + _tilesetExtension, 8, 8);
            Tiles = new Tile[t.Length];
            for (int i = 0; i < t.Length; i++)
            {
                Tiles[i] = new Tile(this, t[i]);
            }
        }

        private const string _tilesetExtension = ".png";
        private const string _tilesetPath = "Tileset.";
        private static readonly IdList _ids = new IdList(_tilesetPath + "TilesetIds.txt");
        private static readonly Dictionary<int, WeakReference<Tileset>> _loadedTilesets = new Dictionary<int, WeakReference<Tileset>>();
        public static Tileset LoadOrGet(int id)
        {
            string name = _ids[id];
            if (name == null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            Tileset t;
            if (!_loadedTilesets.ContainsKey(id))
            {
                t = new Tileset(name);
                _loadedTilesets.Add(id, new WeakReference<Tileset>(t));
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
