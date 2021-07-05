﻿namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        public static void DrawTriangle(uint* dst, int dstW, int dstH, int p0x, int p0y, int p1x, int p1y, int p2x, int p2y, uint color)
        {
            DrawLine(dst, dstW, dstH, p0x, p0y, p1x, p1y, color);
            DrawLine(dst, dstW, dstH, p1x, p1y, p2x, p2y, color);
            DrawLine(dst, dstW, dstH, p2x, p2y, p0x, p0y, color);
        }

        // https://www.avrfreaks.net/sites/default/files/triangles.c
        public static void FillTriangle(uint* dst, int dstW, int dstH, int x1, int y1, int x2, int y2, int x3, int y3, uint color)
        {
            int t1x, t2x, y, minx, maxx, t1xp, t2xp;
            bool changed1 = false;
            bool changed2 = false;
            int signx1, signx2, dx1, dy1, dx2, dy2;
            int e1, e2;
            // Sort vertices
            if (y1 > y2)
            {
                int t = y1;
                y1 = y2;
                y2 = t;
                t = x1;
                x1 = x2;
                x2 = t;
            }
            if (y1 > y3)
            {
                int t = y1;
                y1 = y2;
                y2 = t;
                t = x1;
                x1 = x3;
                x3 = t;
            }
            if (y2 > y3)
            {
                int t = y2;
                y2 = y3;
                y3 = t;
                t = x2;
                x2 = x3;
                x3 = t;
            }

            t1x = t2x = x1; y = y1; // Starting points
            dx1 = x2 - x1;
            if (dx1 < 0)
            {
                dx1 = -dx1;
                signx1 = -1;
            }
            else
            {
                signx1 = 1;
            }

            dy1 = y2 - y1;

            dx2 = x3 - x1;
            if (dx2 < 0)
            {
                dx2 = -dx2;
                signx2 = -1;
            }
            else
            {
                signx2 = 1;
            }

            dy2 = y3 - y1;

            if (dy1 > dx1)
            {
                int t = dx1;
                dx1 = dy1;
                dy1 = t;
                changed1 = true;
            }
            if (dy2 > dx2)
            {
                int t = dy2;
                dy2 = dx2;
                dx2 = t;
                changed2 = true;
            }

            e2 = dx2 >> 1;
            // Flat top, just process the second half
            if (y1 == y2)
            {
                goto next;
            }

            e1 = dx1 >> 1;

            for (int i = 0; i < dx1;)
            {
                t1xp = 0; t2xp = 0;
                if (t1x < t2x)
                {
                    minx = t1x;
                    maxx = t2x;
                }
                else
                {
                    minx = t2x;
                    maxx = t1x;
                }
                // Process first line until y value is about to change
                while (i < dx1)
                {
                    i++;
                    e1 += dy1;
                    while (e1 >= dx1)
                    {
                        e1 -= dx1;
                        if (changed1)
                        {
                            t1xp = signx1;
                        }
                        else
                        {
                            goto next1;
                        }
                    }
                    if (changed1)
                    {
                        break;
                    }
                    else
                    {
                        t1x += signx1;
                    }
                }
            next1:
                // Move line
                // Process second line until y value is about to change
                while (true)
                {
                    e2 += dy2;
                    while (e2 >= dx2)
                    {
                        e2 -= dx2;
                        if (changed2)
                        {
                            t2xp = signx2;
                        }
                        else
                        {
                            goto next2;
                        }
                    }
                    if (changed2)
                    {
                        break;
                    }
                    else
                    {
                        t2x += signx2;
                    }
                }
            next2:
                if (minx > t1x)
                {
                    minx = t1x;
                }
                if (minx > t2x)
                {
                    minx = t2x;
                }
                if (maxx < t1x)
                {
                    maxx = t1x;
                }
                if (maxx < t2x)
                {
                    maxx = t2x;
                }

                DrawHorizontalLine_Points(dst, dstW, dstH, minx, y, maxx, color);    // Draw line from min to max points found on the y
                                                                                     // Now increase y
                if (!changed1)
                {
                    t1x += signx1;
                }
                t1x += t1xp;
                if (!changed2)
                {
                    t2x += signx2;
                }
                t2x += t2xp;
                y += 1;
                if (y == y2)
                {
                    break;
                }
            }
        next:
            // Second half
            dx1 = x3 - x2;
            if (dx1 < 0)
            {
                dx1 = -dx1;
                signx1 = -1;
            }
            else
            {
                signx1 = 1;
            }

            dy1 = y3 - y2;
            t1x = x2;

            if (dy1 > dx1)
            {
                int t = dy1;
                dy1 = dx1;
                dx1 = t;
                changed1 = true;
            }
            else
            {
                changed1 = false;
            }

            e1 = dx1 >> 1;

            for (int i = 0; i <= dx1; i++)
            {
                t1xp = 0; t2xp = 0;
                if (t1x < t2x)
                {
                    minx = t1x;
                    maxx = t2x;
                }
                else
                {
                    minx = t2x;
                    maxx = t1x;
                }
                // Process first line until y value is about to change
                while (i < dx1)
                {
                    e1 += dy1;
                    while (e1 >= dx1)
                    {
                        e1 -= dx1;
                        if (changed1)
                        {
                            t1xp = signx1;
                            break;
                        }
                        else
                        {
                            goto next3;
                        }
                    }
                    if (changed1)
                    {
                        break;
                    }
                    else
                    {
                        t1x += signx1;
                    }

                    if (i < dx1)
                    {
                        i++;
                    }
                }
            next3:
                // Process second line until y value is about to change
                while (t2x != x3)
                {
                    e2 += dy2;
                    while (e2 >= dx2)
                    {
                        e2 -= dx2;
                        if (changed2)
                        {
                            t2xp = signx2;
                        }
                        else
                        {
                            goto next4;
                        }
                    }
                    if (changed2)
                    {
                        break;
                    }
                    else
                    {
                        t2x += signx2;
                    }
                }
            next4:
                if (minx > t1x)
                {
                    minx = t1x;
                }
                if (minx > t2x)
                {
                    minx = t2x;
                }
                if (maxx < t1x)
                {
                    maxx = t1x;
                }
                if (maxx < t2x)
                {
                    maxx = t2x;
                }

                DrawHorizontalLine_Points(dst, dstW, dstH, minx, y, maxx, color);
                if (!changed1)
                {
                    t1x += signx1;
                }
                t1x += t1xp;
                if (!changed2)
                {
                    t2x += signx2;
                }
                t2x += t2xp;
                y += 1;
                if (y > y3)
                {
                    return;
                }
            }
        }
    }
}
