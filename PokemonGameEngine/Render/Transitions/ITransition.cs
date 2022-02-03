using Kermalis.PokemonGameEngine.Render.OpenGL;
using System;

namespace Kermalis.PokemonGameEngine.Render.Transitions
{
    internal interface ITransition : IDisposable
    {
        bool IsDone { get; }

        void Render(FrameBuffer target);
    }
}
