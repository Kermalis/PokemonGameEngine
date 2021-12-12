using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static class RenderManager
    {
        public static void Init()
        {
            GL gl = Display.OpenGL;
            // Init shader instances
            _ = new FontShader(gl);

            // Init other instances
            _ = new GUIRenderer(gl);
            Font.Init();
        }
    }
}
