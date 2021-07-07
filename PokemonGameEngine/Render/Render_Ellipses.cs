using System;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        // http://enchantia.com/graphapp/doc/tech/ellipses.html
        /// <summary>Works based on a center point and with radii. Even radii do not work properly.</summary>
        public static void FillEllipse_Center(uint* dst, int dstW, int dstH, int xCenter, int yCenter, int xRadius, int yRadius, uint color)
        {
            int x = 0,
                y = yRadius;
            int rx = x,
                ry = y;
            int width = 1;
            int height = 1;
            long a2 = Math.BigMul(xRadius, xRadius),
                b2 = Math.BigMul(yRadius, yRadius);
            long crit1 = -((a2 / 4) + (xRadius % 2) + b2);
            long crit2 = -((b2 / 4) + (yRadius % 2) + a2);
            long crit3 = -((b2 / 4) + (yRadius % 2));
            long t = -a2 * y;
            long dxt = 2 * b2 * x,
                dyt = -2 * a2 * y;
            long d2xt = 2 * b2,
                d2yt = 2 * a2;

            if (yRadius == 0)
            {
                FillRectangle(dst, dstW, dstH, xCenter - xRadius, yCenter, (2 * xRadius) + 1, 1, color);
                return;
            }

            void incx()
            {
                x++;
                dxt += d2xt;
                t += dxt;
            }
            void incy()
            {
                y--;
                dyt += d2yt;
                t += dyt;
            }

            while (y >= 0 && x <= xRadius)
            {
                if (t + (b2 * x) <= crit1 || t + (a2 * y) <= crit3)
                {
                    if (height == 1)
                    {
                        // Draw nothing
                    }
                    else if ((ry * 2) + 1 > (height - 1) * 2)
                    {
                        FillRectangle(dst, dstW, dstH, xCenter - rx, yCenter - ry, width, height - 1, color);
                        FillRectangle(dst, dstW, dstH, xCenter - rx, yCenter + ry + 1, width, 1 - height, color);
                        ry -= height - 1;
                        height = 1;
                    }
                    else
                    {
                        FillRectangle(dst, dstW, dstH, xCenter - rx, yCenter - ry, width, (ry * 2) + 1, color);
                        ry -= ry;
                        height = 1;
                    }
                    incx();
                    rx++;
                    width += 2;
                }
                else if (t - (a2 * y) > crit2)
                {
                    incy();
                    height++;
                }
                else
                {
                    if ((ry * 2) + 1 > height * 2)
                    {
                        FillRectangle(dst, dstW, dstH, xCenter - rx, yCenter - ry, width, height, color);
                        FillRectangle(dst, dstW, dstH, xCenter - rx, yCenter + ry + 1 - height, width, height, color);
                    }
                    else
                    {
                        FillRectangle(dst, dstW, dstH, xCenter - rx, yCenter - ry, width, (ry * 2) + 1, color);
                    }
                    incx();
                    incy();
                    rx++;
                    width += 2;
                    ry -= height;
                    height = 1;
                }
            }

            if (ry > height)
            {
                FillRectangle(dst, dstW, dstH, xCenter - rx, yCenter - ry, width, height, color);
                FillRectangle(dst, dstW, dstH, xCenter - rx, yCenter + ry + 1 - height, width, height, color);
            }
            else
            {
                FillRectangle(dst, dstW, dstH, xCenter - rx, yCenter - ry, width, (ry * 2) + 1, color);
            }
        }
        // https://stackoverflow.com/questions/2914807/plot-ellipse-from-rectangle
        /// <summary>Works based on a top-left point and with width and height. Even widths and heights work properly.</summary>
        public static void DrawEllipse_Points(uint* dst, int dstW, int dstH, int x1, int y1, int x2, int y2, uint color)
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
                // Draw the new points
                DrawPoint_Checked(dst, dstW, dstH, xb - dx, yb - dy, color);
                if (dx != 0 || xb != xc)
                {
                    DrawPoint_Checked(dst, dstW, dstH, xc + dx, yb - dy, color);
                    if (dy != 0 || yb != yc)
                    {
                        DrawPoint_Checked(dst, dstW, dstH, xc + dx, yc + dy, color);
                    }
                }
                if (dy != 0 || yb != yc)
                {
                    DrawPoint_Checked(dst, dstW, dstH, xb - dx, yc + dy, color);
                }

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
                    qt += 8L * qa * qa - 4L * qa * qa * qy;
                    dy--;
                    qy -= 2;
                    // Else step (+1, -1)
                }
                else
                {
                    qt += 8L * qb * qb + 4L * qb * qb * qx + 8L * qa * qa - 4L * qa * qa * qy;
                    dx++;
                    qx += 2;
                    dy--;
                    qy -= 2;
                }
            }
        }
        // https://stackoverflow.com/questions/2914807/plot-ellipse-from-rectangle
        /// <summary>Works based on a top-left point and with width and height. Even widths and heights work properly.</summary>
        public static void FillEllipse_Points(uint* dst, int dstW, int dstH, int x1, int y1, int x2, int y2, uint color)
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
