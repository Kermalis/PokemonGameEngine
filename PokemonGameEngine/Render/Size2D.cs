using System.Numerics;

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

        public static Size2D FromRelative(Vector2 v, Size2D totalSize)
        {
            return FromRelative(v.X, v.Y, totalSize);
        }
        public static Size2D FromRelative(float w, float h, Size2D totalSize)
        {
            return new Size2D((uint)(w * totalSize.Width), (uint)(h * totalSize.Height));
        }

        public uint GetArea()
        {
            return Width * Height;
        }

        public static Vector2 operator *(Vector2 a, Size2D b)
        {
            return new Vector2(a.X * b.Width, a.Y * b.Height);
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("[W: {0}, H: {1}]", Width, Height);
        }
#endif
    }
}
