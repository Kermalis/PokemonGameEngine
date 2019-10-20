using Kermalis.MapEditor.Util;
using System.Collections.Generic;

namespace Kermalis.MapEditor.Core
{
    public sealed class Blockset
    {
        public sealed class Block
        {
            public sealed class Tile
            {
                private static System.Random r = new System.Random();
                public byte ZLayer;
                public bool XFlip;
                public bool YFlip;
                public Tileset.Tile TilesetTile = Tileset.LoadOrGet("TestTiles").Tiles[r.Next(36)]; // Default value cuz testing
            }

            public readonly Blockset Parent;
            public List<Tile> TopLeft;
            public List<Tile> TopRight;
            public List<Tile> BottomLeft;
            public List<Tile> BottomRight;
            public ushort Behavior;

            public Block(Blockset parent)
            {
                Parent = parent;
                TopLeft = new List<Tile>() { new Tile(), new Tile() { ZLayer = 1 } };
                TopRight = new List<Tile>() { new Tile() };
                BottomLeft = new List<Tile>() { new Tile() };
                BottomRight = new List<Tile>() { new Tile() };
            }

            public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
            {
                for (int z = 0; z < byte.MaxValue + 1; z++)
                {
                    DrawZ(bmpAddress, bmpWidth, bmpHeight, x, y, z);
                }
            }
            public unsafe void DrawZ(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int z)
            {
                void Draw(List<Tile> layers, int tx, int ty)
                {
                    for (int t = 0; t < layers.Count; t++)
                    {
                        Tile tile = layers[t];
                        if (tile.ZLayer == z)
                        {
                            RenderUtil.Draw(bmpAddress, bmpWidth, bmpHeight, tx, ty, tile.TilesetTile.Colors, tile.XFlip, tile.YFlip);
                        }
                    }
                }
                Draw(TopLeft, x, y);
                Draw(TopRight, x + 8, y);
                Draw(BottomLeft, x, y + 8);
                Draw(BottomRight, x + 8, y + 8);
            }
        }

        private readonly string _name;
        private int _numUses;
        public List<Block> Blocks;

        // TODO: Load from file
        private Blockset(string name)
        {
            _name = name;
            Blocks = new List<Block>();
            for (int i = 0; i < 20; i++)
            {
                Blocks.Add(new Block(this));
            }
        }

        private static readonly Dictionary<string, Blockset> _loadedBlocksets = new Dictionary<string, Blockset>();
        public static Blockset LoadOrGet(string name)
        {
            Blockset b;
            if (_loadedBlocksets.ContainsKey(name))
            {
                b = _loadedBlocksets[name];
            }
            else
            {
                b = new Blockset(name);
                _loadedBlocksets.Add(name, b);
            }
            b._numUses++;
            return b;
        }
        public void DeductReference()
        {
            _numUses--;
            if (_numUses <= 0)
            {
                _loadedBlocksets.Remove(_name);
            }
        }
    }
}
