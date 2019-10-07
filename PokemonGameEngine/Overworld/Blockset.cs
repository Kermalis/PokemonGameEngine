namespace Kermalis.PokemonGameEngine.Overworld
{
    internal sealed class Blockset
    {
        public sealed class Block
        {
            public sealed class Tile
            {
                public byte ZLayer { get; }
                public bool XFlip { get; }
                public bool YFlip { get; }
                public byte TilesetNum { get; }
                public int TilesetTileNum { get; }

                public Tile(int tilesetTileNum, byte zLayer, bool xFlip, bool yFlip)
                {
                    ZLayer = zLayer;
                    XFlip = xFlip;
                    YFlip = yFlip;
                    TilesetTileNum = tilesetTileNum;
                }
            }

            public Tile[] TopLeft { get; }
            public Tile[] TopRight { get; }
            public Tile[] BottomLeft { get; }
            public Tile[] BottomRight { get; }
            public ushort Behavior { get; }

            public Block(Tile[] topLeft, Tile[] topRight, Tile[] bottomLeft, Tile[] bottomRight)
            {
                TopLeft = topLeft;
                TopRight = topRight;
                BottomLeft = bottomLeft;
                BottomRight = bottomRight;
            }
        }

        private readonly Block[] _blocks;

        public Block this[int block] => _blocks[block];

        public Blockset()
        {
            _blocks = new Block[]
            {
                new Block(
                    new[] { new Block.Tile(0, 0, false, false) },
                    new[] { new Block.Tile(1, 0, false, false) },
                    new[] { new Block.Tile(6, 0, false, false) },
                    new[] { new Block.Tile(7, 0, false, false) }
                    ),
                new Block(
                    new[] { new Block.Tile(1, 0, false, false) },
                    new[] { new Block.Tile(1, 0, false, false) },
                    new[] { new Block.Tile(7, 0, false, false) },
                    new[] { new Block.Tile(7, 0, false, false) }
                    ),
                new Block(
                    new[] { new Block.Tile(1, 0, false, false) },
                    new[] { new Block.Tile(2, 0, false, false) },
                    new[] { new Block.Tile(7, 0, false, false) },
                    new[] { new Block.Tile(8, 0, false, false) }
                ),
                new Block(
                    new[] { new Block.Tile(12, 0, false, false) },
                    new[] { new Block.Tile(13, 0, false, false) },
                    new[] { new Block.Tile(12, 0, false, false) },
                    new[] { new Block.Tile(13, 0, false, false) }
                    ),
                new Block(
                    new[] { new Block.Tile(13, 0, false, false) },
                    new[] { new Block.Tile(13, 0, false, false) },
                    new[] { new Block.Tile(13, 0, false, false) },
                    new[] { new Block.Tile(13, 0, false, false) }
                    ),
                new Block(
                    new[] { new Block.Tile(13, 0, false, false) },
                    new[] { new Block.Tile(14, 0, false, false) },
                    new[] { new Block.Tile(13, 0, false, false) },
                    new[] { new Block.Tile(14, 0, false, false) }
                    ),
                new Block(
                    new[] { new Block.Tile(18, 0, false, false) },
                    new[] { new Block.Tile(19, 0, false, false) },
                    new[] { new Block.Tile(24, 0, false, false) },
                    new[] { new Block.Tile(25, 0, false, false) }
                    ),
                new Block(
                    new[] { new Block.Tile(19, 0, false, false) },
                    new[] { new Block.Tile(19, 0, false, false) },
                    new[] { new Block.Tile(3, 0, false, false) },
                    new[] { new Block.Tile(4, 0, false, false) }
                    ),
                new Block(
                    new[] { new Block.Tile(19, 0, false, false) },
                    new[] { new Block.Tile(19, 0, false, false) },
                    new[] { new Block.Tile(5, 0, false, false) },
                    new[] { new Block.Tile(5, 0, true, false) }
                    ),
                new Block(
                    new[] { new Block.Tile(19, 0, false, false) },
                    new[] { new Block.Tile(20, 0, false, false) },
                    new[] { new Block.Tile(25, 0, false, false) },
                    new[] { new Block.Tile(26, 0, false, false) }
                    ),
                new Block(
                    new[] { new Block.Tile(24, 0, false, false) },
                    new[] { new Block.Tile(25, 0, false, false) },
                    new[] { new Block.Tile(30, 0, false, false) },
                    new[] { new Block.Tile(31, 0, false, false) }
                    ),
                new Block(
                    new[] { new Block.Tile(9, 0, false, false) },
                    new[] { new Block.Tile(10, 0, false, false) },
                    new[] { new Block.Tile(15, 0, false, false) },
                    new[] { new Block.Tile(16, 0, false, false) }
                    ),
                new Block(
                    new[] { new Block.Tile(11, 0, false, false) },
                    new[] { new Block.Tile(11, 0, true, false) },
                    new[] { new Block.Tile(33, 0, false, false) },
                    new[] { new Block.Tile(33, 0, false, false) }
                    ),
                new Block(
                    new[] { new Block.Tile(25, 0, false, false) },
                    new[] { new Block.Tile(26, 0, false, false) },
                    new[] { new Block.Tile(31, 0, false, false) },
                    new[] { new Block.Tile(32, 0, false, false) }
                    )
            };
        }
    }
}
