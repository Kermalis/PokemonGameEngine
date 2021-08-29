using Kermalis.PokemonGameEngine.Render.OpenGL;

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

        public static Pos2D FromRelative(float x, float y)
        {
            return FromRelative(x, y, GLHelper.CurrentWidth, GLHelper.CurrentHeight);
        }
        public static Pos2D FromRelative(float x, float y, uint w, uint h)
        {
            return new Pos2D((int)(x * w), (int)(y * h));
        }

        public static Pos2D Center(float centerX, float centerY, Size2D srcSize)
        {
            return new Pos2D(Renderer.GetCoordinatesForCentering(GLHelper.CurrentWidth, srcSize.Width, centerX),
                Renderer.GetCoordinatesForCentering(GLHelper.CurrentHeight, srcSize.Height, centerY));
        }
        public static Pos2D CenterXBottomY(float centerX, float bottomY, Size2D srcSize)
        {
            return new Pos2D(Renderer.GetCoordinatesForCentering(GLHelper.CurrentWidth, srcSize.Width, centerX),
                Renderer.GetCoordinatesForEndAlign(GLHelper.CurrentHeight, srcSize.Height, bottomY));
        }

        public static Pos2D operator +(Pos2D a, Pos2D b)
        {
            return new Pos2D(a.X + b.X, a.Y + b.Y);
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("[X: {0}, Y: {1}]", X, Y);
        }
#endif
    }

    internal struct RelPos2D
    {
        public float X;
        public float Y;

        public RelPos2D(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Pos2D Absolute()
        {
            return Pos2D.FromRelative(X, Y);
        }
        public Pos2D Absolute(Size2D size)
        {
            return Pos2D.FromRelative(X, Y, size.Width, size.Height);
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("[X: {0}, Y: {1}]", X, Y);
        }
#endif
    }

    internal struct Size2D
    {
        public uint Width;
        public uint Height;

        public Size2D(uint w, uint h)
        {
            Width = w;
            Height = h;
        }

        public static Size2D FromRelative(float w, float h)
        {
            return FromRelative(w, h, GLHelper.CurrentWidth, GLHelper.CurrentHeight);
        }
        public static Size2D FromRelative(float w, float h, uint totalW, uint totalH)
        {
            return new Size2D((uint)(w * totalW), (uint)(h * totalH));
        }

        public uint GetArea()
        {
            return Width * Height;
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("[W: {0}, H: {1}]", Width, Height);
        }
#endif
    }

    internal struct RelSize2D
    {
        public float Width;
        public float Height;

        public RelSize2D(float w, float h)
        {
            Width = w;
            Height = h;
        }

        public Size2D Absolute()
        {
            return Size2D.FromRelative(Width, Height);
        }
        public Size2D Absolute(Size2D size)
        {
            return Size2D.FromRelative(Width, Height, size.Width, size.Height);
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("[W: {0}, H: {1}]", Width, Height);
        }
#endif
    }
}
