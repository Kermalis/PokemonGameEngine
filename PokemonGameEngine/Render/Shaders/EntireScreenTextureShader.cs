using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Shaders
{
    internal sealed class EntireScreenTextureShader : GLShader
    {
        private const string VERTEX_SHADER_PATH = @"EntireScreenTexture.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"EntireScreenTexture.frag.glsl";

        public static EntireScreenTextureShader Instance { get; private set; } = null!; // Initialized in RenderManager

        public EntireScreenTextureShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            Instance = this;

            // Set texture unit now
            Use(gl);
            gl.Uniform1(GetUniformLocation(gl, "uTexture"), 0);
        }
    }
}
