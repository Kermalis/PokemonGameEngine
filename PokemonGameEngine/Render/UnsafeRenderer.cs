using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe class UnsafeRenderer
    {
        #region Shapes

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

        // https://stackoverflow.com/questions/2914807/plot-ellipse-from-rectangle
        /// <summary>Works based on a top-left point and with width and height. Even widths and heights work properly.</summary>
        public static void FillEllipse_Points(uint* dst, Size2D dstSize, Rect2D rect, uint color)
        {
            int xb, yb, xc, yc;

            // Calculate height
            int bottom = rect.GetBottom();
            yb = yc = (rect.TopLeft.Y + bottom) / 2;
            int qb = bottom - rect.TopLeft.Y;
            int qy = qb;
            int dy = qb / 2;
            if (qb % 2 != 0)
            {
                // Bounding box has even pixel height
                yc++;
            }

            // Calculate width
            int right = rect.GetRight();
            xb = xc = (rect.TopLeft.X + right) / 2;
            int qa = right - rect.TopLeft.X;
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
                    DrawHorizontalLine_Points(dst, dstSize, xb - dx, yc + dy, xc + dx, color);
                    if (dy != 0 || yb != yc)
                    {
                        DrawHorizontalLine_Points(dst, dstSize, xb - dx, yb - dy, xc + dx, color);
                    }
                    qt += 8L * qa * qa - 4L * qa * qa * qy;
                    dy--;
                    qy -= 2;
                    // Else step (+1, -1)
                }
                else
                {
                    DrawHorizontalLine_Points(dst, dstSize, xb - dx, yc + dy, xc + dx, color);
                    if (dy != 0 || yb != yc)
                    {
                        DrawHorizontalLine_Points(dst, dstSize, xb - dx, yb - dy, xc + dx, color);
                    }
                    qt += 8L * qb * qb + 4L * qb * qb * qx + 8L * qa * qa - 4L * qa * qa * qy;
                    dx++;
                    qx += 2;
                    dy--;
                    qy -= 2;
                }
            }
        }

        #endregion

        #region Raw Drawing

        // Colors must be RGBA8888 (0xAABBCCDD - AA is A, BB is B, CC is G, DD is R)

        public static void DrawPoint_Unchecked(uint* dst, uint color)
        {
            uint aIn = GetA(color);
            if (aIn == 0)
            {
                return; // Fully transparent
            }
            if (aIn == 0xFF)
            {
                *dst = color; // Fully opaque
                return;
            }
            uint rIn = GetR(color);
            uint gIn = GetG(color);
            uint bIn = GetB(color);
            uint current = *dst;
            uint rOld = GetR(current);
            uint gOld = GetG(current);
            uint bOld = GetB(current);
            uint aOld = GetA(current);
            uint r = (rIn * aIn / 0xFF) + (rOld * aOld * (0xFF - aIn) / (0xFF * 0xFF));
            uint g = (gIn * aIn / 0xFF) + (gOld * aOld * (0xFF - aIn) / (0xFF * 0xFF));
            uint b = (bIn * aIn / 0xFF) + (bOld * aOld * (0xFF - aIn) / (0xFF * 0xFF));
            uint a = aIn + (aOld * (0xFF - aIn) / 0xFF);
            *dst = RawColor(r, g, b, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPixelIndex(uint srcW, Pos2D pos)
        {
            return pos.X + (pos.Y * (int)srcW);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint* GetPixelAddress(uint* src, uint srcW, Pos2D pos)
        {
            return src + GetPixelIndex(srcW, pos);
        }

        #endregion

        #region Colors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RawColor(uint r, uint g, uint b, uint a)
        {
            return (a << 24) | (b << 16) | (g << 8) | r;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetR(uint color)
        {
            return color & 0xFF;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetG(uint color)
        {
            return (color >> 8) & 0xFF;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetB(uint color)
        {
            return (color >> 16) & 0xFF;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetA(uint color)
        {
            return color >> 24;
        }

        #endregion
    }
}
