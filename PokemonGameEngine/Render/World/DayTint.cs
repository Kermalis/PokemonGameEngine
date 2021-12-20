using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Shaders;
using Kermalis.PokemonGameEngine.World;
using Silk.NET.OpenGL;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal static class DayTint
    {
        private static int _tintHour;
        private static int _tintMinute;
        private static Vector3 _mod;

        public static bool IsEnabled; // Gets set by CameraObj
        /// <summary>Signals that the next render should instantly be the current daytint with no transition. Will be reset on the next render</summary>
        public static bool CatchUpTime;

        #region Color data

        // TODO: Colors by season
        // TODO: Generate lookup tables from colors to render faster?
        private static readonly Vector3[] _colors = new Vector3[24]
        {
            new(0.160f, 0.180f, 0.330f), // 00
            new(0.160f, 0.180f, 0.330f), // 01
            new(0.160f, 0.180f, 0.330f), // 02
            new(0.170f, 0.185f, 0.345f), // 03
            new(0.225f, 0.235f, 0.375f), // 04
            new(0.350f, 0.265f, 0.415f), // 05
            new(0.500f, 0.400f, 0.500f), // 06
            new(0.720f, 0.660f, 0.555f), // 07
            new(0.900f, 0.785f, 0.815f), // 08
            new(0.950f, 0.980f, 0.905f), // 09
            new(1.000f, 0.985f, 0.945f), // 10
            new(1.000f, 1.000f, 0.950f), // 11
            new(1.000f, 1.000f, 1.000f), // 12
            new(1.000f, 1.000f, 0.985f), // 13
            new(1.000f, 1.000f, 0.955f), // 14
            new(0.995f, 1.000f, 0.950f), // 15
            new(0.955f, 0.975f, 0.850f), // 16
            new(0.845f, 0.885f, 0.740f), // 17
            new(0.700f, 0.690f, 0.560f), // 18
            new(0.545f, 0.460f, 0.390f), // 19
            new(0.490f, 0.320f, 0.380f), // 20
            new(0.250f, 0.235f, 0.370f), // 21
            new(0.180f, 0.205f, 0.350f), // 22
            new(0.160f, 0.180f, 0.330f)  // 23
        };

        #endregion

        public static void SetTintTime()
        {
            DateTime time = DateTime.Now;
            _tintHour = OverworldTime.GetHour(time.Hour);
            _tintMinute = OverworldTime.GetMinute(time.Minute);
        }
        private static bool IsEffectivelyEnabled()
        {
#if DEBUG_DISABLE_DAYTINT
            return false;
#else
            return IsEnabled;
#endif
        }

        private static void Update(bool skipTransition)
        {
            DateTime time = DateTime.Now;
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
            Vector3 hourMod = _colors[tintHour];
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
            Vector3 nextHourMod = _colors[nextTintHour];
            hourMod.X += (nextHourMod.X - hourMod.X) * minuteMod;
            hourMod.Y += (nextHourMod.Y - hourMod.Y) * minuteMod;
            hourMod.Z += (nextHourMod.Z - hourMod.Z) * minuteMod;
            if (nextTintMinute >= 60)
            {
                _tintMinute = 0;
                _tintHour = nextTintHour;
            }
            else
            {
                _tintMinute = nextTintMinute;
            }
            _mod = hourMod;
        }

        public static void Render(FrameBuffer dayTintFrameBuffer)
        {
            bool catchUpTime = CatchUpTime;
            CatchUpTime = false;
            if (!IsEffectivelyEnabled())
            {
                return;
            }
            Update(catchUpTime);

            GL gl = Display.OpenGL;
            DayTintShader shader = DayTintShader.Instance;
            shader.Use(gl);
            shader.SetModification(gl, ref _mod);

            // Bind current fbo's texture
            FrameBuffer c = FrameBuffer.Current;
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, c.ColorTexture);

            // Render to DayTint fbo
            dayTintFrameBuffer.Use();
            EntireScreenMesh.Instance.Render();

            // Copy rendered result back to the previous fbo (its texture is still bound)
            gl.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 0, 0, c.Size.Width, c.Size.Height);
            c.Use();
        }
    }
}
