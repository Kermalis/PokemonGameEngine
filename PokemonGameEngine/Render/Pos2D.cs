using Kermalis.PokemonGameEngine.World;
using System;

namespace Kermalis.PokemonGameEngine.Render
{
    internal struct Pos2D
    {
        public int X;
        public int Y;

        public Pos2D(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Pos2D South(int amt = 1)
        {
            return new Pos2D(X, Y + amt);
        }
        public Pos2D North(int amt = 1)
        {
            return new Pos2D(X, Y - amt);
        }
        public Pos2D West(int amt = 1)
        {
            return new Pos2D(X - amt, Y);
        }
        public Pos2D East(int amt = 1)
        {
            return new Pos2D(X + amt, Y);
        }
        public Pos2D Southwest(int xAmt = 1, int yAmt = 1)
        {
            return new Pos2D(X - xAmt, Y + yAmt);
        }
        public Pos2D Southeast(int xAmt = 1, int yAmt = 1)
        {
            return new Pos2D(X + xAmt, Y + yAmt);
        }
        public Pos2D Northwest(int xAmt = 1, int yAmt = 1)
        {
            return new Pos2D(X - xAmt, Y - yAmt);
        }
        public Pos2D Northeast(int xAmt = 1, int yAmt = 1)
        {
            return new Pos2D(X + xAmt, Y - yAmt);
        }
        public Pos2D Move(FacingDirection dir)
        {
            switch (dir)
            {
                case FacingDirection.South: return South();
                case FacingDirection.North: return North();
                case FacingDirection.West: return West();
                case FacingDirection.East: return East();
                case FacingDirection.Southwest: return Southwest();
                case FacingDirection.Southeast: return Southeast();
                case FacingDirection.Northwest: return Northwest();
                case FacingDirection.Northeast: return Northeast();
                default: throw new ArgumentOutOfRangeException(nameof(dir));
            }
        }

        public static Pos2D FromRelative(float x, float y, Size2D totalSize)
        {
            return new Pos2D((int)(x * totalSize.Width), (int)(y * totalSize.Height));
        }

        public static Pos2D FromSheet(uint imgIndex, Size2D imgSize, uint atlasWidth)
        {
            uint i = imgIndex * imgSize.Width;
            uint x = i % atlasWidth;
            uint y = i / atlasWidth * imgSize.Height;
            return new Pos2D((int)x, (int)y);
        }

        public static Pos2D Center(float centerX, float centerY, Size2D srcSize, Size2D totalSize)
        {
            return new Pos2D(RenderUtils.GetCoordinatesForCentering(totalSize.Width, srcSize.Width, centerX),
                RenderUtils.GetCoordinatesForCentering(totalSize.Height, srcSize.Height, centerY));
        }
        public static Pos2D CenterXBottomY(float centerX, float bottomY, Size2D srcSize, Size2D totalSize)
        {
            return new Pos2D(RenderUtils.GetCoordinatesForCentering(totalSize.Width, srcSize.Width, centerX),
                RenderUtils.GetCoordinatesForEndAlign(totalSize.Height, srcSize.Height, bottomY));
        }

        public static Pos2D operator +(Pos2D a, Pos2D b)
        {
            return new Pos2D(a.X + b.X, a.Y + b.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is Pos2D other && X == other.X && Y == other.Y;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("[X: {0}, Y: {1}]", X, Y);
        }
#endif
    }
}
