namespace Kermalis.PokemonGameEngine.Render
{
    internal static partial class RenderUtils
    {
        // Mimics DrawLineHigh
        public static unsafe void ThreeColorBackground(uint* bmpAddress, int bmpWidth, int bmpHeight, uint color1, uint color2, uint color3)
        {
            const float wf = 0.12f;
            const float x1f = 0.45f;
            const float x2f = 0.15f;
            int w = (int)(wf * bmpWidth);
            int x1 = (int)(x1f * bmpWidth);
            int x2 = (int)(x2f * bmpWidth);
            int y1 = 0;
            int y2 = bmpHeight - 1;

            // DrawLineHigh essentially
            int dx = x2 - x1;
            int dy = y2 - y1;
            int xi = 1;
            if (dx < 0)
            {
                xi = -1;
                dx = -dx;
            }
            int d = 2 * dx - dy;
            int px = x1;
            for (int py = y1; py <= y2; py++)
            {
                DrawHorizontalLine_Points(bmpAddress, bmpWidth, bmpHeight, 0, py, px - 1, color1);
                DrawHorizontalLine_Points(bmpAddress, bmpWidth, bmpHeight, px, py, px + w, color2);
                DrawHorizontalLine_Points(bmpAddress, bmpWidth, bmpHeight, px + w + 1, py, bmpWidth - 1, color3);
                if (d > 0)
                {
                    px += xi;
                    d -= 2 * dy;
                }
                d += 2 * dx;
            }
        }
    }
}
