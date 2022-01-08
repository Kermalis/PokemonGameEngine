using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    /// <summary>Helps with writing to pixel arrays. Colors are RGBA8888 (0xAABBCCDD - AA is Alpha, BB is Blue, CC is Green, DD is Red)</summary>
    internal static unsafe class UnsafeRenderer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RawColor(uint r, uint g, uint b, uint a)
        {
            return (a << 24) | (b << 16) | (g << 8) | r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPixelIndex(int imgW, Vec2I pos)
        {
            return pos.X + (pos.Y * imgW);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint* GetPixelAddress(uint* img, int imgW, Vec2I pos)
        {
            return img + GetPixelIndex(imgW, pos);
        }

        #region Shapes

        private static void DrawHorizontalLine(uint* dst, int dstW, int x1, int y, int x2, uint color)
        {
            Vec2I pos;
            pos.Y = y;
            for (pos.X = x1; pos.X <= x2; pos.X++)
            {
                *GetPixelAddress(dst, dstW, pos) = color;
            }
        }

        // https://stackoverflow.com/questions/2914807/plot-ellipse-from-rectangle
        public static void FillEllipse(uint* dst, int dstW, in Rect rect, uint color)
        {
            int xb, yb, xc, yc;

            // Calculate height
            int bottom = rect.BottomRight.Y;
            yb = yc = (rect.TopLeft.Y + bottom) / 2;
            int qb = bottom - rect.TopLeft.Y;
            int qy = qb;
            int dy = qb / 2;
            if (qb % 2 != 0)
            {
                yc++; // Bounding box has even pixel height
            }

            // Calculate width
            int right = rect.BottomRight.X;
            xb = xc = (rect.TopLeft.X + right) / 2;
            int qa = right - rect.TopLeft.X;
            int qx = qa % 2;
            int dx = 0;
            long qt = ((long)qa * qa) + ((long)qb * qb) - (2L * qa * qa * qb);
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
                if (qt + (2L * qb * qb * qx) + (3L * qb * qb) <= 0L ||
                    qt + (2L * qa * qa * qy) - ((long)qa * qa) <= 0L)
                {
                    qt += (8L * qb * qb) + (4L * qb * qb * qx);
                    dx++;
                    qx += 2;
                    // If a (0, -1) step stays outside the ellipse, do it
                }
                else if (qt - (2L * qa * qa * qy) + (3L * qa * qa) > 0L)
                {
                    DrawHorizontalLine(dst, dstW, xb - dx, yc + dy, xc + dx, color);
                    if (dy != 0 || yb != yc)
                    {
                        DrawHorizontalLine(dst, dstW, xb - dx, yb - dy, xc + dx, color);
                    }
                    qt += (8L * qa * qa) - (4L * qa * qa * qy);
                    dy--;
                    qy -= 2;
                    // Else step (+1, -1)
                }
                else
                {
                    DrawHorizontalLine(dst, dstW, xb - dx, yc + dy, xc + dx, color);
                    if (dy != 0 || yb != yc)
                    {
                        DrawHorizontalLine(dst, dstW, xb - dx, yb - dy, xc + dx, color);
                    }
                    qt += (8L * qb * qb) + (4L * qb * qb * qx) + (8L * qa * qa) - (4L * qa * qa * qy);
                    dx++;
                    qx += 2;
                    dy--;
                    qy -= 2;
                }
            }
        }

        #endregion
    }
}
