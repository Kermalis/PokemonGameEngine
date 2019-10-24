using Kermalis.MapEditor.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    internal sealed class Tileset
    {
        public sealed class Tile
        {
            public readonly Tileset Parent;
            public readonly int Id;
            public readonly uint[][] Colors;

            public Tile(Tileset parent, int id, uint[][] colors)
            {
                Parent = parent;
                Id = id;
                Colors = colors;
            }
        }

        private static readonly IdList _ids = new IdList(Path.Combine(Program.AssetPath, "Tileset", "TilesetIds.txt"));

        public readonly int Id;
        public readonly Tile[] Tiles;

        private Tileset(string name, int id)
        {
            uint[][][] t = RenderUtil.LoadSpriteSheet(Path.Combine(Program.AssetPath, "Tileset", name + ".png"), 8, 8);
            Tiles = new Tile[t.Length];
            for (int i = 0; i < t.Length; i++)
            {
                Tiles[i] = new Tile(this, i, t[i]);
            }
            Id = id;
        }

        private static readonly List<WeakReference<Tileset>> _loadedTilesets = new List<WeakReference<Tileset>>();
        public static Tileset LoadOrGet(string name)
        {
            int id = _ids[name];
            if (id == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            return LoadOrGet(name, id);
        }
        public static Tileset LoadOrGet(int id)
        {
            string name = _ids[id];
            if (name == null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            return LoadOrGet(name, id);
        }
        private static Tileset LoadOrGet(string name, int id)
        {
            Tileset t;
            if (id >= _loadedTilesets.Count)
            {
                t = new Tileset(name, id);
                _loadedTilesets.Add(new WeakReference<Tileset>(t));
                return t;
            }
            if (_loadedTilesets[id].TryGetTarget(out t))
            {
                return t;
            }
            t = new Tileset(name, id);
            _loadedTilesets[id].SetTarget(t);
            return t;
        }
    }
}
