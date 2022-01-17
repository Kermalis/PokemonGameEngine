using Kermalis.PokemonGameEngine.World;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render
{
    internal struct Vec2I
    {
        public int X;
        public int Y;

        public Vec2I(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int GetArea()
        {
            return X * Y;
        }

        public Vec2I Plus(int x, int y)
        {
            return new Vec2I(X + x, Y + y);
        }
        public Vec2I Move(FacingDirection dir)
        {
            switch (dir)
            {
                case FacingDirection.South: return Plus(0, 1);
                case FacingDirection.North: return Plus(0, -1);
                case FacingDirection.West: return Plus(-1, 0);
                case FacingDirection.East: return Plus(1, 0);
                case FacingDirection.Southwest: return Plus(-1, 1);
                case FacingDirection.Southeast: return Plus(1, 1);
                case FacingDirection.Northwest: return Plus(-1, -1);
                case FacingDirection.Northeast: return Plus(1, -1);
                default: throw new ArgumentOutOfRangeException(nameof(dir));
            }
        }

        public static Vec2I FromRelative(float x, float y, Vec2I totalSize)
        {
            return (Vec2I)(new Vector2(x, y) * totalSize);
        }

        public static Vec2I Center(float centerX, float centerY, Vec2I srcSize, Vec2I totalSize)
        {
            return new Vec2I(RenderUtils.GetCoordinatesForCentering(totalSize.X, srcSize.X, centerX),
                RenderUtils.GetCoordinatesForCentering(totalSize.Y, srcSize.Y, centerY));
        }
        public static Vec2I CenterXRelativeY(float centerX, float relativeY, int srcWidth, Vec2I totalSize)
        {
            return new Vec2I(RenderUtils.GetCoordinatesForCentering(totalSize.X, srcWidth, centerX),
                (int)(relativeY * totalSize.Y));
        }
        public static Vec2I CenterXBottomY(float centerX, float bottomY, Vec2I srcSize, Vec2I totalSize)
        {
            return new Vec2I(RenderUtils.GetCoordinatesForCentering(totalSize.X, srcSize.X, centerX),
                RenderUtils.GetCoordinatesForEndAlign(totalSize.Y, srcSize.Y, bottomY));
        }

        #region Operators

        public static implicit operator Vector2(Vec2I v)
        {
            return new Vector2(v.X, v.Y);
        }
        public static explicit operator Vec2I(Vector2 v)
        {
            return new Vec2I((int)v.X, (int)v.Y);
        }

        public static Vec2I operator -(Vec2I v)
        {
            return new Vec2I(-v.X, -v.Y);
        }
        public static Vec2I operator +(Vec2I a, Vec2I b)
        {
            return new Vec2I(a.X + b.X, a.Y + b.Y);
        }
        public static Vec2I operator -(Vec2I a, Vec2I b)
        {
            return new Vec2I(a.X - b.X, a.Y - b.Y);
        }
        public static Vec2I operator *(Vec2I a, Vec2I b)
        {
            return new Vec2I(a.X * b.X, a.Y * b.Y);
        }
        public static Vec2I operator /(Vec2I a, Vec2I b)
        {
            return new Vec2I(a.X / b.X, a.Y / b.Y);
        }
        public static Vec2I operator %(Vec2I a, Vec2I b)
        {
            return new Vec2I(a.X % b.X, a.Y % b.Y);
        }

        public static Vec2I operator *(Vec2I a, int b)
        {
            return new Vec2I(a.X * b, a.Y * b);
        }
        public static Vector2 operator *(Vec2I a, float b)
        {
            return new Vector2(a.X * b, a.Y * b);
        }
        public static Vec2I operator /(Vec2I a, int b)
        {
            return new Vec2I(a.X / b, a.Y / b);
        }

        public static bool operator ==(Vec2I a, Vec2I b)
        {
            return a.X == b.X && a.Y == b.Y;
        }
        public static bool operator !=(Vec2I a, Vec2I b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Vec2I other && other == this;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        #endregion

#if DEBUG
        public override string ToString()
        {
            return string.Format("[X: {0}, Y: {1}]", X, Y);
        }
#endif
    }
}
