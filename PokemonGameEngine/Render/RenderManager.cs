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
            _ = new BattleTransitionShader_Liquid(gl);
            _ = new BlocksetBlockShader(gl);
            _ = new DayTintShader(gl);
            _ = new EntireScreenTextureShader(gl);
            _ = new FadeColorShader(gl);
            _ = new FontShader(gl);
            _ = new GUIRectShader(gl);

            // Init other instances
            RectMesh.Instance = new RectMesh(gl);
            _ = new TripleColorBackgroundMesh(gl);
            Blockset.Init();
            Font.Init();
        }
    }
}
