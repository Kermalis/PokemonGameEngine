using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;

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

        #region HP & EXP

        public static unsafe void HP_TripleLine(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int width, double percent)
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
            DrawRectangle(bmpAddress, bmpWidth, bmpHeight, x, y, width, 5, Color(49, 49, 49, 255));
            FillRectangle(bmpAddress, bmpWidth, bmpHeight, x + 1, y + 1, width - 2, 3, Color(33, 33, 33, 255));
            int theW = (int)((width - 2) * percent);
            if (theW == 0 && percent > 0)
            {
                theW = 1;
            }
            DrawHorizontalLine_Width(bmpAddress, bmpWidth, bmpHeight, x + 1, y + 1, theW, hpSides);
            DrawHorizontalLine_Width(bmpAddress, bmpWidth, bmpHeight, x + 1, y + 2, theW, hpMid);
            DrawHorizontalLine_Width(bmpAddress, bmpWidth, bmpHeight, x + 1, y + 3, theW, hpSides);
        }

        public static unsafe void EXP_SingleLine(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int width, uint exp, byte level, PBESpecies species, PBEForm form)
        {
            if (level >= PkmnConstants.MaxLevel)
            {
                EXP_SingleLine(bmpAddress, bmpWidth, bmpHeight, x, y, width, 0);
                return;
            }
            PBEGrowthRate gr = new BaseStats(species, form).GrowthRate;
            EXP_SingleLine(bmpAddress, bmpWidth, bmpHeight, x, y, width, exp, level, gr);
        }
        public static unsafe void EXP_SingleLine(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int width, uint exp, byte level, PBEGrowthRate gr)
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
            EXP_SingleLine(bmpAddress, bmpWidth, bmpHeight, x, y, width, percent);
        }
        public static unsafe void EXP_SingleLine(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int width, double percent)
        {
            DrawRectangle(bmpAddress, bmpWidth, bmpHeight, x, y, width, 3, Color(49, 49, 49, 255));
            DrawHorizontalLine_Width(bmpAddress, bmpWidth, bmpHeight, x + 1, y + 1, width - 2, Color(33, 33, 33, 255));
            int theW = (int)((width - 2) * percent);
            if (theW == 0 && percent > 0)
            {
                theW = 1;
            }
            DrawHorizontalLine_Width(bmpAddress, bmpWidth, bmpHeight, x + 1, y + 1, theW, Color(0, 160, 255, 255));
        }

        #endregion
    }
}
