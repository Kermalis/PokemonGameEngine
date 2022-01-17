using Kermalis.PokemonGameEngine.World;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Shaders.World
{
    internal sealed class MapLayoutShader : Shader2D
    {
        private const string VERTEX_SHADER_PATH = @"World\MapLayout.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"World\MapLayout.frag.glsl";

        public MapLayoutShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            // Set texture uniform right away
            Use(gl);
            gl.Uniform1(GetUniformLocation(gl, "blocksetTexture"), 0);
            gl.Uniform2(GetUniformLocation(gl, "blockSize"), Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY);
        }
    }
}
