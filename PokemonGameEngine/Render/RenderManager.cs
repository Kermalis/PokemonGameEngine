using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Shaders;
using Kermalis.PokemonGameEngine.Render.Shaders.GUIs;
using Kermalis.PokemonGameEngine.Render.Shaders.Transitions;
using Kermalis.PokemonGameEngine.Render.Shaders.World;
using Kermalis.PokemonGameEngine.Render.World;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static class RenderManager
    {
        public static void Init()
        {
            GL gl = Display.OpenGL;
            // Init shader instances
            _ = new BlocksetBlockShader(gl);
            _ = new EntireScreenTextureShader(gl);
            _ = new FontShader(gl);
            _ = new DayTintShader(gl);
            _ = new FadeColorShader(gl);
            _ = new BattleTransitionShader_Liquid(gl);

            // Init other instances
            _ = new GUIRenderer(gl);
            _ = new RectMesh(gl);
            _ = new MapLayoutBlockMesh(gl);
            _ = new TripleColorBackgroundMesh(gl);
            Blockset.Init();
            Font.Init();
        }
    }
}
