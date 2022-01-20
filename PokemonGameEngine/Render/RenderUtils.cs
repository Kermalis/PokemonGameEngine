using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static class RenderUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TextureUnit ToTextureUnit(this int unit)
        {
            return (TextureUnit)((int)TextureUnit.Texture0 + unit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCoordinatesForCentering(int dstSize, int srcSize, float pos)
        {
            return (int)(dstSize * pos) - (srcSize / 2);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCoordinatesForEndAlign(int dstSize, int srcSize, float pos)
        {
            return (int)(dstSize * pos) - srcSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearColor(this GL gl, in Vector3 color)
        {
            gl.ClearColor(color.X, color.Y, color.Z, 1f);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearColor(this GL gl, in Vector4 color)
        {
            gl.ClearColor(color.X, color.Y, color.Z, color.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2I DecideGridElementPos(Vec2I availableSpace, Vec2I colsRows, Vec2I spacing, int i)
        {
            var iFactor = new Vec2I(i % colsRows.X, i / colsRows.X);
            return (availableSpace / colsRows * iFactor) + spacing;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2I DecideGridElementSize(Vec2I availableSpace, Vec2I colsRows, Vec2I spacing)
        {
            return (availableSpace / colsRows) - (spacing * 2);
        }

        #region Specials

        public static void HP_TripleLine(Vec2I pos, int width, float percent)
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
            GUIRenderer.Rect(Colors.V4FromRGB(49, 49, 49), Rect.FromSize(pos, new Vec2I(width, 5)), lineThickness: 1);
            GUIRenderer.Rect(Colors.V4FromRGB(33, 33, 33), Rect.FromSize(pos.Plus(1, 1), new Vec2I(width - 2, 3)));

            Vec2I size;
            size.X = (int)((width - 2) * percent);
            if (size.X == 0 && percent > 0)
            {
                size.X = 1;
            }
            size.Y = 1;

            GUIRenderer.Rect(hpSides, Rect.FromSize(pos.Plus(1, 1), size));
            GUIRenderer.Rect(hpMid, Rect.FromSize(pos.Plus(1, 2), size));
            GUIRenderer.Rect(hpSides, Rect.FromSize(pos.Plus(1, 3), size));
        }

        public static void EXP_SingleLine(Vec2I pos, int width, uint exp, byte level, PBESpecies species, PBEForm form)
        {
            if (level >= PkmnConstants.MaxLevel)
            {
                EXP_SingleLine(pos, width, 0);
                return;
            }
            PBEGrowthRate gr = BaseStats.Get(species, form, true).GrowthRate;
            EXP_SingleLine(pos, width, exp, level, gr);
        }
        public static void EXP_SingleLine(Vec2I pos, int width, uint exp, byte level, PBEGrowthRate gr)
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
        public static void EXP_SingleLine(Vec2I pos, int width, float percent)
        {
            GUIRenderer.Rect(Colors.V4FromRGB(33, 33, 33), Colors.V4FromRGB(49, 49, 49), Rect.FromSize(pos, new Vec2I(width, 3)), 1);

            Vec2I size;
            size.X = (int)((width - 2) * percent);
            if (size.X == 0 && percent > 0)
            {
                size.X = 1;
            }
            size.Y = 1;
            GUIRenderer.Rect(Colors.V4FromRGB(0, 160, 255), Rect.FromSize(pos.Plus(1, 1), size));
        }

        #endregion
    }
}
