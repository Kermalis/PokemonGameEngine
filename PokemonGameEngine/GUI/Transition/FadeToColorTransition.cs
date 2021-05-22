using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class FadeToColorTransition : FadeColorTransition
    {
        private TimeSpan _cur;
        private readonly TimeSpan _end;
        private readonly uint _color;

        public FadeToColorTransition(int totalMilliseconds, uint color)
        {
            _cur = new TimeSpan();
            _end = TimeSpan.FromMilliseconds(totalMilliseconds);
            _color = color;
        }

        public unsafe override void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            if (IsDone)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.SetA(_color, 0xFF));
                return;
            }
            double progress = Utils.GetAnimationProgress(_end, ref _cur);
            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.SetA(_color, (uint)(progress * 0xFF)));

            if (!IsDone && progress >= 1)
            {
                IsDone = true;
            }
        }
    }
}
