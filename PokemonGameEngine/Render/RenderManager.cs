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
            _ = new EntireScreenTextureShader(gl);
            _ = new FontShader(gl);
            _ = new DayTintShader(gl);
            _ = new FadeColorShader(gl);
            _ = new BattleTransitionShader_Liquid(gl);

            // Init other instances
            _ = new GUIRenderer(gl);
            _ = new EntireScreenMesh(gl);
            _ = new TripleColorBackgroundMesh(gl);
            Font.Init();
        }
    }
}
