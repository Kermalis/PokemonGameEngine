using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Shaders;
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
            _ = new DayTintShader(gl);

            // Init other instances
            _ = new GUIRenderer(gl);
            _ = new SimpleRectMesh(gl);
            Font.Init();
        }
    }
}
