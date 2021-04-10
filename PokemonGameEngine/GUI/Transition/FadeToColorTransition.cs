using Kermalis.PokemonGameEngine.Render;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class FadeToColorTransition : FadeColorTransition
    {
        private readonly int _transitionDuration;
        private readonly float _transitionDurationF;
        private readonly uint _color;

        private int _counter;

        public FadeToColorTransition(int transitionDuration, uint color)
        {
            _transitionDuration = transitionDuration;
            _transitionDurationF = transitionDuration;
            _color = color;
        }

        public unsafe override void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.SetA(_color, (uint)(_counter / _transitionDurationF * 0xFF)));

            if (!IsDone && _counter++ >= _transitionDuration)
            {
                _counter = _transitionDuration;
                IsDone = true;
            }
        }
    }
}
