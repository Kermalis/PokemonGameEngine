using System;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal interface ITransition : IDisposable
    {
        bool IsDone { get; }

        void Render();
    }
}
