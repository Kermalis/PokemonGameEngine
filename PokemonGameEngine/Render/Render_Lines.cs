using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        public static void OverwriteHorizontalLine_Width(uint* dst, int dstW, int dstH, int x, int y, int width, uint color)
        {
            if (y < 0 || y >= dstH)
            {
                return;
            }
            int target = x + width;
            for (int px = x; px < target; px++)
            {
                if (px >= 0 && px < dstW)
                {
                    *GetPixelAddress(dst, dstW, px, y) = color;
                }
            }
        }
        public static void OverwriteHorizontalLine_Points(uint* dst, int dstW, int dstH, int x1, int y, int x2, uint color)
        {
            if (y < 0 || y >= dstH)
            {
                return;
            }
            for (int px = x1; px <= x2; px++)
            {
                if (px >= 0 && px < dstW)
                {
                    *GetPixelAddress(dst, dstW, px, y) = color;
                }
            }
        }
        public static void OverwriteVerticalLine_Height(uint* dst, int dstW, int dstH, int x, int y, int height, uint color)
        {
            if (x < 0 || x >= dstW)
            {
                return;
            }
            int target = y + height;
            for (int py = y; py < target; py++)
            {
                if (py >= 0 && py < dstH)
                {
                    *GetPixelAddress(dst, dstW, x, py) = color;
                }
            }
        }
        public static void OverwriteVerticalLine_Points(uint* dst, int dstW, int dstH, int x, int y1, int y2, uint color)
        {
            if (x < 0 || x >= dstW)
            {
                return;
            }
            for (int py = y1; py <= y2; py++)
            {
                if (py >= 0 && py < dstH)
                {
                    *GetPixelAddress(dst, dstW, x, py) = color;
                }
            }
        }

        public static void DrawHorizontalLine_Width(uint* dst, int dstW, int dstH, int x, int y, int width, uint color)
        {
            if (y < 0 || y >= dstH)
            {
                return;
            }
            int target = x + width;
            for (int px = x; px < target; px++)
            {
                if (px >= 0 && px < dstW)
                {
                    DrawPoint_Unchecked(GetPixelAddress(dst, dstW, px, y), color);
                }
            }
        }
        public static void DrawHorizontalLine_Points(uint* dst, int dstW, int dstH, int x1, int y, int x2, uint color)
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
        public static void DrawVerticalLine_Height(uint* dst, int dstW, int dstH, int x, int y, int height, uint color)
        {
            if (x < 0 || x >= dstW)
            {
                return;
            }
            int target = y + height;
            for (int py = y; py < target; py++)
            {
                if (py >= 0 && py < dstH)
                {
                    DrawPoint_Unchecked(GetPixelAddress(dst, dstW, x, py), color);
                }
            }
        }
        public static void DrawVerticalLine_Points(uint* dst, int dstW, int dstH, int x, int y1, int y2, uint color)
        {
            if (x < 0 || x >= dstW)
            {
                return;
            }
            for (int py = y1; py <= y2; py++)
            {
                if (py >= 0 && py < dstH)
                {
                    DrawPoint_Unchecked(GetPixelAddress(dst, dstW, x, py), color);
                }
            }
        }

        // Bresenham's line algorithm
        // The following link has a way to do this with thick lines: https://github.com/ArminJo/STMF3-Discovery-Demos/blob/master/lib/graphics/src/thickLine.cpp
        // I honestly don't understand any of it and don't need it now.
        public static void DrawLineLow(uint* dst, int dstW, int dstH, int x1, int y1, int x2, int y2, uint color)
        {
            int dx = x2 - x1;
            int dy = y2 - y1;
            int yi = 1;
            if (dy < 0)
            {
                yi = -1;
                dy = -dy;
            }
            int d = 2 * dy - dx;
            int py = y1;
            for (int px = x1; px <= x2; px++)
            {
                DrawPoint_Checked(dst, dstW, dstH, px, py, color);
                if (d > 0)
                {
                    py += yi;
                    d -= 2 * dx;
                }
                d += 2 * dy;
            }
        }
        public static void DrawLineHigh(uint* dst, int dstW, int dstH, int x1, int y1, int x2, int y2, uint color)
        {
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
                DrawPoint_Checked(dst, dstW, dstH, px, py, color);
                if (d > 0)
                {
                    px += xi;
                    d -= 2 * dy;
                }
                d += 2 * dx;
            }
        }
        public static void DrawLine(uint* dst, int dstW, int dstH, int x1, int y1, int x2, int y2, uint color)
        {
            if (x1 == x2)
            {
                if (y1 == y2)
                {
                    DrawPoint_Checked(dst, dstW, dstH, x1, y1, color);
                    return;
                }
                if (y1 > y2)
                {
                    int bak = y1;
                    y1 = y2;
                    y2 = bak;
                }
                DrawVerticalLine_Points(dst, dstW, dstH, x1, y1, y2, color);
                return;
            }
            if (y1 == y2)
            {
                if (x1 > x2)
                {
                    int bak = x1;
                    x1 = x2;
                    x2 = bak;
                }
                DrawHorizontalLine_Points(dst, dstW, dstH, x1, y1, x2, color);
                return;
            }
            if (Math.Abs(y2 - y1) < Math.Abs(x2 - x1))
            {
                if (x1 > x2)
                {
                    DrawLineLow(dst, dstW, dstH, x2, y2, x1, y1, color);
                }
                else
                {
                    DrawLineLow(dst, dstW, dstH, x1, y1, x2, y2, color);
                }
            }
            else
            {
                if (y1 > y2)
                {
                    DrawLineHigh(dst, dstW, dstH, x2, y2, x1, y1, color);
                }
                else
                {
                    DrawLineHigh(dst, dstW, dstH, x1, y1, x2, y2, color);
                }
            }
        }
    }
}
