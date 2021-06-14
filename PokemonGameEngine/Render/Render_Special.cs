using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        // Mimics DrawLineHigh
        public static void ThreeColorBackground(uint* dst, int dstW, int dstH, uint color1, uint color2, uint color3)
        {
            const float wf = 0.12f;
            const float x1f = 0.45f;
            const float x2f = 0.15f;
            int w = (int)(wf * dstW);
            int x1 = (int)(x1f * dstW);
            int x2 = (int)(x2f * dstW);
            int y1 = 0;
            int y2 = dstH - 1;

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
                DrawHorizontalLine_Points(dst, dstW, dstH, 0, py, px - 1, color1);
                DrawHorizontalLine_Points(dst, dstW, dstH, px, py, px + w, color2);
                DrawHorizontalLine_Points(dst, dstW, dstH, px + w + 1, py, dstW - 1, color3);
                if (d > 0)
                {
                    px += xi;
                    d -= 2 * dy;
                }
                d += 2 * dx;
            }
        }

        #region HP & EXP

        public static void HP_TripleLine(uint* dst, int dstW, int dstH, int x, int y, int width, double percent)
        {
            uint hpSides, hpMid;
            if (percent <= 0.20)
            {
                hpSides = Color(148, 33, 49, 255);
                hpMid = Color(255, 49, 66, 255);
            }
            else if (percent <= 0.50)
            {
                hpSides = Color(156, 99, 16, 255);
                hpMid = Color(247, 181, 0, 255);
            }
            else
            {
                hpSides = Color(0, 140, 41, 255);
                hpMid = Color(0, 255, 74, 255);
            }
            DrawRectangle(dst, dstW, dstH, x, y, width, 5, Color(49, 49, 49, 255));
            FillRectangle(dst, dstW, dstH, x + 1, y + 1, width - 2, 3, Color(33, 33, 33, 255));
            int theW = (int)((width - 2) * percent);
            if (theW == 0 && percent > 0)
            {
                theW = 1;
            }
            DrawHorizontalLine_Width(dst, dstW, dstH, x + 1, y + 1, theW, hpSides);
            DrawHorizontalLine_Width(dst, dstW, dstH, x + 1, y + 2, theW, hpMid);
            DrawHorizontalLine_Width(dst, dstW, dstH, x + 1, y + 3, theW, hpSides);
        }

        public static void EXP_SingleLine(uint* dst, int dstW, int dstH, int x, int y, int width, uint exp, byte level, PBESpecies species, PBEForm form)
        {
            if (level >= PkmnConstants.MaxLevel)
            {
                EXP_SingleLine(dst, dstW, dstH, x, y, width, 0);
                return;
            }
            PBEGrowthRate gr = BaseStats.Get(species, form, true).GrowthRate;
            EXP_SingleLine(dst, dstW, dstH, x, y, width, exp, level, gr);
        }
        public static void EXP_SingleLine(uint* dst, int dstW, int dstH, int x, int y, int width, uint exp, byte level, PBEGrowthRate gr)
        {
            double percent;
            if (level >= PkmnConstants.MaxLevel)
            {
                percent = 0;
            }
            else
            {
                uint expPrev = PBEEXPTables.GetEXPRequired(gr, level);
                uint expNext = PBEEXPTables.GetEXPRequired(gr, (byte)(level + 1));
                uint expCur = exp;
                percent = (double)(expCur - expPrev) / (expNext - expPrev);
            }
            EXP_SingleLine(dst, dstW, dstH, x, y, width, percent);
        }
        public static void EXP_SingleLine(uint* dst, int dstW, int dstH, int x, int y, int width, double percent)
        {
            DrawRectangle(dst, dstW, dstH, x, y, width, 3, Color(49, 49, 49, 255));
            DrawHorizontalLine_Width(dst, dstW, dstH, x + 1, y + 1, width - 2, Color(33, 33, 33, 255));
            int theW = (int)((width - 2) * percent);
            if (theW == 0 && percent > 0)
            {
                theW = 1;
            }
            DrawHorizontalLine_Width(dst, dstW, dstH, x + 1, y + 1, theW, Color(0, 160, 255, 255));
        }

        #endregion

        public static void Sprite_DrawWithShadow(Sprite s, uint* dst, int dstW, int dstH, int xOffset = 0, int yOffset = 0)
        {
            if (s.IsInvisible)
            {
                return;
            }

            fixed (uint* src = s.Image.Bitmap)
            {
                DrawBitmapWithShadow(dst, dstW, dstH, s.X + xOffset, s.Y + yOffset, src, s.Image.Width, s.Image.Height);
            }
        }
        public static PixelSupplier MakeShadowSupplier(uint* src, int srcW)
        {
            return (x, y) =>
            {
                uint color = *GetPixelAddress(src, srcW, x, y);
                return color != 0 ? Color(0, 0, 0, 160) : color;
            };
        }
        public static void DrawBitmapWithShadow(uint* dst, int dstW, int dstH, int x, int y, uint* src, int srcW, int srcH, bool xFlip = false, bool yFlip = false)
        {
            PixelSupplier pixSupplySrc = MakeBitmapSupplier(src, srcW);
            PixelSupplier pixSupplyShadow = MakeShadowSupplier(src, srcW);
            fixed (uint* rotated = CreateRotatedBitmap(pixSupplyShadow, srcW, srcH, 5, out int rotWidth, out int rotHeight, out _, out _, xFlip: xFlip, yFlip: yFlip, smooth: true))
            {
                pixSupplyShadow = MakeBitmapSupplier(rotated, rotWidth);
                int shadowW = (int)(rotWidth * 0.95f);
                int shadowH = (int)(rotHeight * 0.6f);
                int shadowX = x + (int)(srcW * 0.035f);
                int shadowY = y + srcH - shadowH - (int)(srcH * 0.04f);
                DrawBitmapSized(dst, dstW, dstH, shadowX, shadowY, shadowW, shadowH, pixSupplyShadow, rotWidth, rotHeight);
                DrawBitmap(dst, dstW, dstH, x, y, pixSupplySrc, srcW, srcH, xFlip: xFlip, yFlip: yFlip);
            }
        }
    }
}
