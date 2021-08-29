namespace Kermalis.PokemonGameEngine.Render
{
    internal struct Rect2D
    {
        public Pos2D TopLeft;
        public Size2D Size;

        public Rect2D(Pos2D topLeft, Size2D size)
        {
            TopLeft = topLeft;
            Size = size;
        }
        public Rect2D(Pos2D topLeft, Pos2D bottomRight)
        {
            TopLeft = topLeft;
            Size = new Size2D((uint)(bottomRight.X - topLeft.X + 1), (uint)(bottomRight.Y - topLeft.Y + 1));
        }

        public int GetRight()
        {
            return TopLeft.X + (int)Size.Width - 1;
        }
        public void SetRight(int x)
        {
            Size.Width = (uint)(x - TopLeft.X + 1);
        }
        public int GetBottom()
        {
            return TopLeft.Y + (int)Size.Height - 1;
        }
        public void SetBottom(int y)
        {
            Size.Height = (uint)(y - TopLeft.Y + 1);
        }

        public Pos2D GetTopRight()
        {
            return new Pos2D(GetRight(), TopLeft.Y);
        }
        public void SetTopRight(Pos2D topRight)
        {
            TopLeft.Y = topRight.Y;
            SetRight(topRight.X);
        }
        public Pos2D GetBottomLeft()
        {
            return new Pos2D(TopLeft.X, GetBottom());
        }
        public void SetBottomLeft(Pos2D bottomLeft)
        {
            TopLeft.X = bottomLeft.X;
            SetBottom(bottomLeft.Y);
        }
        public Pos2D GetBottomRight()
        {
            return new Pos2D(GetRight(), GetBottom());
        }
        public void SetBottomRight(Pos2D bottomRight)
        {
            SetRight(bottomRight.X);
            SetBottom(bottomRight.Y);
        }

        public bool Intersects(Size2D sizeFrom00)
        {
            return TopLeft.X < sizeFrom00.Width && TopLeft.Y < sizeFrom00.Height
                && GetRight() >= 0 && GetBottom() >= 0;
        }
        public bool Intersects(Rect2D other)
        {
            return TopLeft.X <= other.GetRight() && TopLeft.Y <= other.GetBottom()
                && GetRight() >= other.TopLeft.X && GetBottom() >= other.TopLeft.Y;
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("[{0}, {1}]", TopLeft, Size);
        }
#endif
    }
}
