namespace Kermalis.PokemonGameEngine.Overworld
{
    internal sealed class Block
    {
        public sealed class Tile
        {
            public int TileNum { get; }
            public byte ZLayer { get; }
            public bool XFlip { get; }
            public bool YFlip { get; }

            public Tile(int tileNum, byte zLayer, bool xFlip, bool yFlip)
            {
                TileNum = tileNum;
                ZLayer = zLayer;
                XFlip = xFlip;
                YFlip = yFlip;
            }
        }

        public Tile[] TopLeft { get; }
        public Tile[] TopRight { get; }
        public Tile[] BottomLeft { get; }
        public Tile[] BottomRight { get; }

        public Block(Tile[] topLeft, Tile[] topRight, Tile[] bottomLeft, Tile[] bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }
    }
}
