using Kermalis.PokemonGameEngine.Render;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class FadeToColorTransition
    {
        private readonly int _transitionDuration;
        private readonly float _transitionDurationF;
        private readonly uint _color;
        private Action _onTransitionEnded;

        private int _counter;

        public FadeToColorTransition(int transitionDuration, uint color, Action onTransitionEnded)
        {
            _transitionDuration = transitionDuration;
            _transitionDurationF = transitionDuration;
            _color = color;
            _onTransitionEnded = onTransitionEnded;
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.FillColor(bmpAddress, bmpWidth, bmpHeight, RenderUtils.SetA(_color, (uint)(_counter / _transitionDurationF * 0xFF)));

            if (_counter++ >= _transitionDuration)
            {
                _onTransitionEnded.Invoke();
                _onTransitionEnded = null;
                return;
            }
        }
    }
}
