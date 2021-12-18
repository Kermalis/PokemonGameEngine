using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class FadeFromColorTransition : FadeColorTransition
    {
        private readonly float _duration;
        private readonly Vector4 _color;
        private float _time;

        public FadeFromColorTransition(float seconds, in Vector4 color)
        {
            _duration = seconds;
            _color = color;
        }

        public static FadeFromColorTransition FromBlackStandard()
        {
            return new FadeFromColorTransition(0.5f, Colors.Black4);
        }

        public override void Render()
        {
            if (IsDone)
            {
                return;
            }

            _time += Display.DeltaTime;
            float progress = _time / _duration;
            if (progress >= 1f)
            {
                progress = 1f;
                IsDone = true;
            }

            Vector4 c = _color;
            c.W = -progress + 1f; // Modify alpha
            GUIRenderer.Instance.FillRectangle(c, new Rect2D(new Pos2D(0, 0), FrameBuffer.Current.Size));
        }
    }
}
