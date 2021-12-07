namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        public static void DrawHorizontalLine_Points(uint* dst, Size2D dstSize, int x1, int y, int x2, uint color)
        {
            if (y < 0 || y >= dstSize.Height)
            {
                return;
            }
            for (int px = x1; px <= x2; px++)
            {
                if (px >= 0 && px < dstSize.Width)
                {
                    DrawPoint_Unchecked(GetPixelAddress(dst, dstSize.Width, px, y), color);
                }
            }
        }
    }
}
