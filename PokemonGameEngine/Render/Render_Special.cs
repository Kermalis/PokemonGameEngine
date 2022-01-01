﻿using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render.GUIs;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        public static void HP_TripleLine(Pos2D pos, uint width, float percent)
        {
            Vector4 hpSides, hpMid;
            if (percent <= 0.20)
            {
                hpSides = Colors.V4FromRGB(148, 33, 49);
                hpMid = Colors.V4FromRGB(255, 49, 66);
            }
            else if (percent <= 0.50)
            {
                hpSides = Colors.V4FromRGB(156, 99, 16);
                hpMid = Colors.V4FromRGB(247, 181, 0);
            }
            else
            {
                hpSides = Colors.V4FromRGB(0, 140, 41);
                hpMid = Colors.V4FromRGB(0, 255, 74);
            }
            GUIRenderer.Instance.DrawRectangle(Colors.V4FromRGB(49, 49, 49), new Rect2D(pos, new Size2D(width, 5)));
            GUIRenderer.Instance.FillRectangle(Colors.V4FromRGB(33, 33, 33), new Rect2D(pos.Move(1, 1), new Size2D(width - 2, 3)));
            uint theW = (uint)((width - 2) * percent);
            if (theW == 0 && percent > 0)
            {
                theW = 1;
            }
            GUIRenderer.Instance.DrawHorizontalLine_Width(hpSides, pos.Move(1, 1), theW);
            GUIRenderer.Instance.DrawHorizontalLine_Width(hpMid, pos.Move(1, 2), theW);
            GUIRenderer.Instance.DrawHorizontalLine_Width(hpSides, pos.Move(1, 3), theW);
        }

        public static void EXP_SingleLine(Pos2D pos, uint width, uint exp, byte level, PBESpecies species, PBEForm form)
        {
            if (level >= PkmnConstants.MaxLevel)
            {
                EXP_SingleLine(pos, width, 0);
                return;
            }
            PBEGrowthRate gr = BaseStats.Get(species, form, true).GrowthRate;
            EXP_SingleLine(pos, width, exp, level, gr);
        }
        public static void EXP_SingleLine(Pos2D pos, uint width, uint exp, byte level, PBEGrowthRate gr)
        {
            float percent;
            if (level >= PkmnConstants.MaxLevel)
            {
                percent = 0;
            }
            else
            {
                uint expPrev = PBEDataProvider.Instance.GetEXPRequired(gr, level);
                uint expNext = PBEDataProvider.Instance.GetEXPRequired(gr, (byte)(level + 1));
                uint expCur = exp;
                percent = (float)(expCur - expPrev) / (expNext - expPrev);
            }
            EXP_SingleLine(pos, width, percent);
        }
        public static void EXP_SingleLine(Pos2D pos, uint width, float percent)
        {
            GUIRenderer.Instance.DrawRectangle(Colors.V4FromRGB(49, 49, 49), new Rect2D(pos, new Size2D(width, 3)));
            GUIRenderer.Instance.DrawHorizontalLine_Width(Colors.V4FromRGB(33, 33, 33), pos.Move(1, 1), width - 2);
            uint theW = (uint)((width - 2) * percent);
            if (theW == 0 && percent > 0)
            {
                theW = 1;
            }
            GUIRenderer.Instance.DrawHorizontalLine_Width(Colors.V4FromRGB(0, 160, 255), pos.Move(1, 1), theW);
        }
    }
}
