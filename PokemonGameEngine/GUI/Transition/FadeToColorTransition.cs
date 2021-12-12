using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.GUIs;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class FadeToColorTransition : FadeColorTransition
    {
        private readonly float _duration;
        private readonly Vector4 _color;
        private float _time;

        public FadeToColorTransition(float seconds, in Vector4 color)
        {
            _duration = seconds;
            _color = color;
        }

        public static FadeToColorTransition ToBlackStandard()
        {
            return new FadeToColorTransition(0.5f, Colors.Black4);
        }

        public override void Render()
        {
            Vector4 c = _color;
            if (!IsDone)
            {
                _time += Display.DeltaTime;
                float progress = _time / _duration;
                if (progress >= 1f)
                {
                    progress = 1f;
                    IsDone = true;
                }

                c.W = progress; // Set alpha
            }
            GUIRenderer.Instance.FillRectangle(c, new Rect2D(new Pos2D(0, 0), Size2D.FromRelative(1f, 1f)));
        }
    }
}
