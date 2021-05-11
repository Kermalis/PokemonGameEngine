using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.UI;
using Kermalis.PokemonGameEngine.World;
using System;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal static class DayTint
    {
        private static int _tintHour;
        private static int _tintMinute;
        private static float _rMod;
        private static float _gMod;
        private static float _bMod;

        static DayTint()
        {
            SetTintTime();
        }

        // TODO: Colors by season
        // TODO: Generate lookup tables from colors to render faster?
        private static readonly float[][] _colors = new float[24][]
        {
            new float[3] { 0.160f, 0.180f, 0.330f }, // 00
            new float[3] { 0.160f, 0.180f, 0.330f }, // 01
            new float[3] { 0.160f, 0.180f, 0.330f }, // 02
            new float[3] { 0.170f, 0.185f, 0.345f }, // 03
            new float[3] { 0.225f, 0.235f, 0.375f }, // 04
            new float[3] { 0.350f, 0.265f, 0.415f }, // 05
            new float[3] { 0.500f, 0.400f, 0.500f }, // 06
            new float[3] { 0.720f, 0.660f, 0.555f }, // 07
            new float[3] { 0.900f, 0.785f, 0.815f }, // 08
            new float[3] { 0.950f, 0.980f, 0.905f }, // 09
            new float[3] { 1.000f, 0.985f, 0.945f }, // 10
            new float[3] { 1.000f, 1.000f, 0.950f }, // 11
            new float[3] { 1.000f, 1.000f, 1.000f }, // 12
            new float[3] { 1.000f, 1.000f, 0.985f }, // 13
            new float[3] { 1.000f, 1.000f, 0.955f }, // 14
            new float[3] { 0.995f, 1.000f, 0.950f }, // 15
            new float[3] { 0.955f, 0.975f, 0.850f }, // 16
            new float[3] { 0.845f, 0.885f, 0.740f }, // 17
            new float[3] { 0.700f, 0.690f, 0.560f }, // 18
            new float[3] { 0.545f, 0.460f, 0.390f }, // 19
            new float[3] { 0.490f, 0.320f, 0.380f }, // 20
            new float[3] { 0.250f, 0.235f, 0.370f }, // 21
            new float[3] { 0.180f, 0.205f, 0.350f }, // 22
            new float[3] { 0.160f, 0.180f, 0.330f }  // 23
        };

        private static void SetTintTime()
        {
            DateTime time = Program.LogicTickTime;
            _tintHour = OverworldTime.GetHour(time.Hour);
            _tintMinute = OverworldTime.GetMinute(time.Minute);
        }

        private static void GetHourTint(int hour, out float rMod, out float gMod, out float bMod)
        {
            float[] hourColors = _colors[hour];
            rMod = hourColors[0];
            gMod = hourColors[1];
            bMod = hourColors[2];
        }
        public static void LogicTick(bool skipTransition)
        {
            DateTime time = Program.LogicTickTime;
            int realMinute = OverworldTime.GetMinute(time.Minute);
            int realHour = OverworldTime.GetHour(time.Hour);
            int tintHour;
            if (skipTransition)
            {
                _tintHour = tintHour = realHour;
            }
            else
            {
                tintHour = _tintHour;
            }
            GetHourTint(tintHour, out float rMod, out float gMod, out float bMod);
            // Do minute transition
            int tintMinute;
            if (skipTransition)
            {
                _tintMinute = tintMinute = realMinute;
            }
            else
            {
                tintMinute = _tintMinute;
            }
            float minuteMod = tintMinute / 60f;
            int nextTintMinute = tintMinute;
            if (tintMinute != realMinute || tintHour != realHour)
            {
                nextTintMinute++;
            }
            int nextTintHour = tintHour + 1;
            if (nextTintHour >= 24)
            {
                nextTintHour = 0;
            }
            GetHourTint(nextTintHour, out float nextRMod, out float nextGMod, out float nextBMod);
            rMod += (nextRMod - rMod) * minuteMod;
            gMod += (nextGMod - gMod) * minuteMod;
            bMod += (nextBMod - bMod) * minuteMod;
            if (nextTintMinute >= 60)
            {
                _tintMinute = 0;
                _tintHour = nextTintHour;
            }
            else
            {
                _tintMinute = nextTintMinute;
            }
            _rMod = rMod;
            _gMod = gMod;
            _bMod = bMod;
        }

        public static unsafe void Render(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.ModulateRectangle(bmpAddress, bmpWidth, bmpHeight, _rMod, _gMod, _bMod, 1);
        }
    }
}
