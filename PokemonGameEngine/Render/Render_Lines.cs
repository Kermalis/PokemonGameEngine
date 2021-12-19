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

            Pos2D pos;
            pos.Y = y;
            for (pos.X = x1; pos.X <= x2; pos.X++)
            {
                if (pos.X >= 0 && pos.X < dstSize.Width)
                {
                    DrawPoint_Unchecked(GetPixelAddress(dst, dstSize.Width, pos), color);
                }
            }
        }
    }
}
