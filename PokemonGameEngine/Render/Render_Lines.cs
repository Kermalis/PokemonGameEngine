namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        public static void DrawHorizontalLine_Points(uint* dst, uint dstW, uint dstH, int x1, int y, int x2, uint color)
        {
            if (y < 0 || y >= dstH)
            {
                return;
            }
            for (int px = x1; px <= x2; px++)
            {
                if (px >= 0 && px < dstW)
                {
                    DrawPoint_Unchecked(GetPixelAddress(dst, dstW, px, y), color);
                }
            }
        }
    }
}
