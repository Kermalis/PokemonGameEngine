using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class FadeFromColorTransition : FadeColorTransition
    {
        private TimeSpan _cur;
        private readonly TimeSpan _end;
        private readonly uint _color;

        public FadeFromColorTransition(int totalMilliseconds, uint color)
        {
            _cur = new TimeSpan();
            _end = TimeSpan.FromMilliseconds(totalMilliseconds);
            _color = color;
        }

        public unsafe override void Render(uint* dst, int dstW, int dstH)
        {
            if (IsDone)
            {
                return;
            }
            double progress = Utils.GetAnimationProgress(_end, ref _cur);
            Renderer.FillRectangle(dst, dstW, dstH, Renderer.SetA(_color, (uint)((-progress + 1) * 0xFF)));

            if (!IsDone && progress >= 1)
            {
                IsDone = true;
            }
        }
    }
}
