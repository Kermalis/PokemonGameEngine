using Kermalis.PokemonGameEngine.Render.OpenGL;

namespace Kermalis.PokemonGameEngine.Render
{
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
            return FromRelative(w, h, FrameBuffer.Current.Size);
        }
        public static Size2D FromRelative(float w, float h, Size2D totalSize)
        {
            return new Size2D((uint)(w * totalSize.Width), (uint)(h * totalSize.Height));
        }

        public uint GetArea()
        {
            return Width * Height;
        }
        public Size2D PowerOfTwoize()
        {
            return new Size2D(Renderer.PowerOfTwoize(Width), Renderer.PowerOfTwoize(Height));
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
            return Size2D.FromRelative(Width, Height, size);
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("[W: {0}, H: {1}]", Width, Height);
        }
#endif
    }
}
