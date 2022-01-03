using Kermalis.PokemonGameEngine.World;
using System;
using System.Numerics;

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

        public Pos2D Move(int x, int y)
        {
            return new Pos2D(X + x, Y + y);
        }
        public Pos2D Move(FacingDirection dir)
        {
            switch (dir)
            {
                case FacingDirection.South: return Move(0, 1);
                case FacingDirection.North: return Move(0, -1);
                case FacingDirection.West: return Move(-1, 0);
                case FacingDirection.East: return Move(1, 0);
                case FacingDirection.Southwest: return Move(-1, 1);
                case FacingDirection.Southeast: return Move(1, 1);
                case FacingDirection.Northwest: return Move(-1, -1);
                case FacingDirection.Northeast: return Move(1, -1);
                default: throw new ArgumentOutOfRangeException(nameof(dir));
            }
        }

        public static Pos2D FromRelative(Vector2 v, Size2D totalSize)
        {
            return FromRelative(v.X, v.Y, totalSize);
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
        public static Pos2D operator *(Pos2D a, int b)
        {
            return new Pos2D(a.X * b, a.Y * b);
        }
        public static Pos2D operator -(Pos2D a, Pos2D b)
        {
            return new Pos2D(a.X - b.X, a.Y - b.Y);
        }
        public static Pos2D operator *(Pos2D a, Size2D b)
        {
            return new Pos2D(a.X * (int)b.Width, a.Y * (int)b.Height);
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
