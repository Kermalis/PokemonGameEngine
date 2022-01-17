using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.World;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Shaders.World
{
    internal sealed class BlocksetBlockShader : GLShader
    {
        public static BlocksetBlockShader Instance { get; private set; } = null!; // Initialized in RenderManager

        private const string VERTEX_SHADER_PATH = @"World\BlocksetBlock.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"World\BlocksetBlock.frag.glsl";

        public BlocksetBlockShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            Instance = this;

            // Set tileset texture uniforms right away
            Use(gl);
            for (int i = 0; i < GLTextureUtils.MAX_ACTIVE_TEXTURES; i++)
            {
                gl.Uniform1(GetUniformLocation(gl, "tilesetTextures[" + i + ']'), i);
            }
            gl.Uniform2(GetUniformLocation(gl, "blockSize"), Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY);
        }
    }
}
