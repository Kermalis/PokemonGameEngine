namespace Kermalis.PokemonGameEngine.Render
{
    internal struct Rect
    {
        public Vec2I TopLeft;
        public Vec2I BottomRight;

        private Rect(Vec2I topLeft, Vec2I bottomRight)
        {
            TopLeft = topLeft;
            BottomRight = bottomRight;
        }

        public static Rect FromCorners(Vec2I topLeft, Vec2I bottomRight)
        {
            return new Rect(topLeft, bottomRight);
        }
        public static Rect FromSize(Vec2I topLeft, Vec2I size)
        {
            return new Rect(topLeft, new Vec2I(topLeft.X + size.X - 1, topLeft.Y + size.Y - 1));
        }

        public int GetWidth()
        {
            return BottomRight.X - TopLeft.X + 1;
        }
        public int GetHeight()
        {
            return BottomRight.Y - TopLeft.Y + 1;
        }
        public Vec2I GetSize()
        {
            return new Vec2I(GetWidth(), GetHeight());
        }

        public int GetExclusiveRight()
        {
            return BottomRight.X + 1;
        }
        public int GetExclusiveBottom()
        {
            return BottomRight.Y + 1;
        }
        public Vec2I GetExclusiveBottomLeft()
        {
            return new Vec2I(TopLeft.X, GetExclusiveBottom());
        }
        public Vec2I GetExclusiveTopRight()
        {
            return new Vec2I(GetExclusiveRight(), TopLeft.Y);
        }
        public Vec2I GetExclusiveBottomRight()
        {
            return new Vec2I(GetExclusiveRight(), GetExclusiveBottom());
        }

        public bool Contains(Vec2I pos)
        {
            return pos.X >= TopLeft.X && pos.Y >= TopLeft.Y
                && pos.X <= BottomRight.X && pos.Y <= BottomRight.Y;
        }
        /// <summary>Checks if any point in the rect lies within a rect of (0, 0, size-1, size-1)</summary>
        public bool Intersects(Vec2I sizeFrom00)
        {
            return TopLeft.X < sizeFrom00.X && TopLeft.Y < sizeFrom00.Y
                && BottomRight.X >= 0 && BottomRight.Y >= 0;
        }
        /// <summary>Checks if any point in the rect lies within the other rect</summary>
        public bool Intersects(in Rect other)
        {
            return TopLeft.X <= other.BottomRight.X && TopLeft.Y <= other.BottomRight.Y
                && BottomRight.X >= other.TopLeft.X && BottomRight.Y >= other.TopLeft.Y;
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("[TopLeft: {0}, BottomRight: {1}]", TopLeft, BottomRight);
        }
#endif
    }
}
