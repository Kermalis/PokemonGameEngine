﻿using Kermalis.PokemonGameEngine.Render;
using Silk.NET.OpenGL;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class FadeToColorTransition : FadeColorTransition
    {
        private TimeSpan _cur;
        private readonly TimeSpan _end;
        private readonly ColorF _color;

        public FadeToColorTransition(int totalMilliseconds, in ColorF color)
        {
            _cur = new TimeSpan();
            _end = TimeSpan.FromMilliseconds(totalMilliseconds);
            _color = color;
        }

        public override void Render(GL gl)
        {
            if (IsDone)
            {
                GUIRenderer.Instance.FillRectangle(_color, new Rect2D(new Pos2D(0, 0), Size2D.FromRelative(1f, 1f)));
                return;
            }
            float progress = (float)Renderer.GetAnimationProgress(_end, ref _cur);
            ColorF c = _color;
            c.A = progress;
            GUIRenderer.Instance.FillRectangle(c, new Rect2D(new Pos2D(0, 0), Size2D.FromRelative(1f, 1f)));

            if (!IsDone && progress >= 1)
            {
                IsDone = true;
            }
        }
    }
}
