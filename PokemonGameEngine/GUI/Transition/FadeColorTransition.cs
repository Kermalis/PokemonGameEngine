﻿namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal abstract class FadeColorTransition
    {
        public bool IsDone { get; protected set; }

        public abstract unsafe void Render(uint* dst, int dstW, int dstH);
    }
}
