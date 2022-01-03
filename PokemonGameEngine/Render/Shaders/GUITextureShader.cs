using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Shaders
{
    internal sealed class GUITextureShader : Shader2D
    {
        private const string VERTEX_SHADER_PATH = @"GUI_Texture.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"GUI_Texture.frag.glsl";

        private readonly int _lGUITexture;

        public GUITextureShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            _lGUITexture = GetUniformLocation(gl, "guiTexture");

            // Set texture unit now
            Use(gl);
            gl.Uniform1(_lGUITexture, 0);
        }
    }
}
