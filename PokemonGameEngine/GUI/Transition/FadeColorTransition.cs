using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal abstract class FadeColorTransition
    {
        public bool IsDone { get; protected set; }

        public abstract void Render(GL gl);
    }
}
