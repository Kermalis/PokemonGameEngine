using Kermalis.MapEditor.Util;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    public sealed class Tileset
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

        private readonly string _name;
        private int _numUses;
        public readonly Tile[] Tiles;

        private Tileset(string name)
        {
            _name = name;
            uint[][][] t = RenderUtil.LoadSpriteSheet(Path.Combine(Program.AssetPath, "Tileset", name + ".png"), 8, 8);
            Tiles = new Tile[t.Length];
            for (int i = 0; i < t.Length; i++)
            {
                Tiles[i] = new Tile(this, t[i]);
            }
        }

        private static readonly Dictionary<string, Tileset> _loadedTilesets = new Dictionary<string, Tileset>();
        public static Tileset LoadOrGet(string name)
        {
            Tileset b;
            if (_loadedTilesets.ContainsKey(name))
            {
                b = _loadedTilesets[name];
            }
            else
            {
                b = new Tileset(name);
            }
            b._numUses++;
            return b;
        }
        public void UnloadIfUnused()
        {
            _numUses--;
            if (_numUses <= 0)
            {
                _loadedTilesets.Remove(_name);
            }
        }
    }
}
