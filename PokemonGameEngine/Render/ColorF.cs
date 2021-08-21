namespace Kermalis.PokemonGameEngine.Render
{
    internal struct ColorF
    {
        public float R;
        public float G;
        public float B;
        public float A;

        public ColorF(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static ColorF FromRGB(uint r, uint g, uint b)
        {
            return new ColorF(r / 255f, g / 255f, b / 255f, 1f);
        }
        public static ColorF FromRGBA(uint r, uint g, uint b, uint a)
        {
            return new ColorF(r / 255f, g / 255f, b / 255f, a / 255f);
        }
    }

    internal static class Colors
    {
        public static ColorF Transparent { get; } = new ColorF(0, 0, 0, 0);
        public static ColorF Black { get; } = new ColorF(0, 0, 0, 1);
        public static ColorF White { get; } = new ColorF(1, 1, 1, 1);
        public static ColorF Red { get; } = new ColorF(1, 0, 0, 1);
        public static ColorF Green { get; } = new ColorF(0, 1, 0, 1);
        public static ColorF Blue { get; } = new ColorF(0, 0, 1, 1);
    }
}
