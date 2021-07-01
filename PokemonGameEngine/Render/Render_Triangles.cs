namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        public static void DrawTriangle(uint* dst, int dstW, int dstH, int p0x, int p0y, int p1x, int p1y, int p2x, int p2y, uint color)
        {
            DrawLine(dst, dstW, dstH, p0x, p0y, p1x, p1y, color);
            DrawLine(dst, dstW, dstH, p1x, p1y, p2x, p2y, color);
            DrawLine(dst, dstW, dstH, p2x, p2y, p0x, p0y, color);
        }

        /// <summary>Requires that p0y is the top and p1y and p2y are the bottom</summary>
        public static void FillBottomFlatTriangle(uint* dst, int dstW, int dstH, int p0x, int p0y, int p1x, int p1y, int p2x, int p2y, uint color)
        {
            float invslope1 = (float)(p1x - p0x) / (p1y - p0y);
            float invslope2 = (float)(p2x - p0x) / (p2y - p0y);

            float curx1 = p0x;
            float curx2 = p0x;

            for (int scanlineY = p0y; scanlineY <= p1y; scanlineY++)
            {
                DrawLine(dst, dstW, dstH, (int)curx1, scanlineY, (int)curx2, scanlineY, color);
                curx1 += invslope1;
                curx2 += invslope2;
            }
        }
        /// <summary>Requires that p1y and p2y are the top and p2y is the bottom</summary>
        public static void FillTopFlatTriangle(uint* dst, int dstW, int dstH, int p0x, int p0y, int p1x, int p1y, int p2x, int p2y, uint color)
        {
            float invslope1 = (float)(p2x - p0x) / (p2y - p0y);
            float invslope2 = (float)(p2x - p1x) / (p2y - p1y);

            float curx1 = p2x;
            float curx2 = p2x;

            for (int scanlineY = p2y; scanlineY > p0y; scanlineY--)
            {
                DrawLine(dst, dstW, dstH, (int)curx1, scanlineY, (int)curx2, scanlineY, color);
                curx1 -= invslope1;
                curx2 -= invslope2;
            }
        }

        /// <summary>Requires that p0y <= p1y <= p2y</summary>
        public static void FillTriangle(uint* dst, int dstW, int dstH, int p0x, int p0y, int p1x, int p1y, int p2x, int p2y, uint color)
        {
            if (p1y == p2y)
            {
                FillBottomFlatTriangle(dst, dstW, dstH, p0x, p0y, p1x, p1y, p2x, p2y, color);
                return;
            }
            if (p0y == p1y)
            {
                FillTopFlatTriangle(dst, dstW, dstH, p0x, p0y, p1x, p1y, p2x, p2y, color);
                return;
            }
            // General case - split the triangle into a top-flat and bottom-flat
            int p4x = (int)(p0x + ((float)(p1y - p0y) / (p2y - p0y) * (p2x - p0x)));
            FillBottomFlatTriangle(dst, dstW, dstH, p0x, p0y, p1x, p1y, p4x, p1y, color);
            FillTopFlatTriangle(dst, dstW, dstH, p1x, p1y, p4x, p1y, p2x, p2y, color);
        }
    }
}
