using Kermalis.PokemonGameEngine.Util;
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
            RenderUtil.FillColor(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight, ((uint)(_counter / _transitionDurationF * 0xFF) << 24) + _color);

            if (_counter++ >= _transitionDuration)
            {
                _onTransitionEnded.Invoke();
                _onTransitionEnded = null;
                return;
            }
        }
    }
}
