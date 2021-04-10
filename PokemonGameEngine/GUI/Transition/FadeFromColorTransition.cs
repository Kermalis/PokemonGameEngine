using Kermalis.PokemonGameEngine.Render;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class FadeFromColorTransition : FadeColorTransition
    {
        private readonly float _transitionDurationF;
        private readonly uint _color;

        private int _counter;

        public FadeFromColorTransition(int transitionDuration, uint color)
        {
            _transitionDurationF = transitionDuration;
            _counter = transitionDuration;
            _color = color;
        }

        public unsafe override void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.SetA(_color, (uint)(_counter / _transitionDurationF * 0xFF)));

            if (!IsDone && _counter-- <= 0)
            {
                _counter = 0;
                IsDone = true;
            }
        }
    }
}
