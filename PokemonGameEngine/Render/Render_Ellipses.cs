namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        // https://stackoverflow.com/questions/2914807/plot-ellipse-from-rectangle
        /// <summary>Works based on a top-left point and with width and height. Even widths and heights work properly.</summary>
        public static void FillEllipse_Points(uint* dst, uint dstW, uint dstH, int x1, int y1, int x2, int y2, uint color)
        {
            int xb, yb, xc, yc;

            // Calculate height
            yb = yc = (y1 + y2) / 2;
            int qb = (y1 < y2) ? (y2 - y1) : (y1 - y2);
            int qy = qb;
            int dy = qb / 2;
            if (qb % 2 != 0)
            {
                // Bounding box has even pixel height
                yc++;
            }

            // Calculate width
            xb = xc = (x1 + x2) / 2;
            int qa = (x1 < x2) ? (x2 - x1) : (x1 - x2);
            int qx = qa % 2;
            int dx = 0;
            long qt = (long)qa*qa + (long)qb*qb -2L*qa*qa*qb;
            if (qx != 0)
            {
                // Bounding box has even pixel width
                xc++;
                qt += 3L * qb * qb;
            }

            // Start at (dx, dy) = (0, b) and iterate until (a, 0) is reached
            while (qy >= 0 && qx <= qa)
            {
                // If a (+1, 0) step stays inside the ellipse, do it
                if (qt + 2L * qb * qb * qx + 3L * qb * qb <= 0L ||
                    qt + 2L * qa * qa * qy - (long)qa * qa <= 0L)
                {
                    qt += 8L * qb * qb + 4L * qb * qb * qx;
                    dx++;
                    qx += 2;
                    // If a (0, -1) step stays outside the ellipse, do it
                }
                else if (qt - 2L * qa * qa * qy + 3L * qa * qa > 0L)
                {
                    DrawHorizontalLine_Points(dst, dstW, dstH, xb - dx, yc + dy, xc + dx, color);
                    if (dy != 0 || yb != yc)
                    {
                        DrawHorizontalLine_Points(dst, dstW, dstH, xb - dx, yb - dy, xc + dx, color);
                    }
                    qt += 8L * qa * qa - 4L * qa * qa * qy;
                    dy--;
                    qy -= 2;
                    // Else step (+1, -1)
                }
                else
                {
                    DrawHorizontalLine_Points(dst, dstW, dstH, xb - dx, yc + dy, xc + dx, color);
                    if (dy != 0 || yb != yc)
                    {
                        DrawHorizontalLine_Points(dst, dstW, dstH, xb - dx, yb - dy, xc + dx, color);
                    }
                    qt += 8L * qb * qb + 4L * qb * qb * qx + 8L * qa * qa - 4L * qa * qa * qy;
                    dx++;
                    qx += 2;
                    dy--;
                    qy -= 2;
                }
            }
        }
    }
}
