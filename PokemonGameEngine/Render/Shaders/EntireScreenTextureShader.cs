using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Shaders
{
    internal sealed class EntireScreenTextureShader : GLShader
    {
        private const string VERTEX_SHADER_PATH = @"EntireScreenTexture.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"GUIs\GUI_Texture.frag.glsl";

        public static EntireScreenTextureShader Instance { get; private set; } = null!; // Initialized in RenderManager

        private readonly int _lGuiTexture;

        public EntireScreenTextureShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            Instance = this;

            _lGuiTexture = GetUniformLocation(gl, "guiTexture");

            // Set texture unit now
            Use(gl);
            gl.Uniform1(_lGuiTexture, 0);
        }
    }
}
