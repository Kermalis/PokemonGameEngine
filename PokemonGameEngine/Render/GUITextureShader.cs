using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render
{
    internal sealed class GUITextureShader : Shader2D
    {
        private readonly int _lGUITexture;

        public GUITextureShader(GL gl)
            : base(gl, "Shaders.gui_texture_vert.glsl", "Shaders.gui_texture_frag.glsl")
        {
            _lGUITexture = GetUniformLocation(gl, "guiTexture");
        }

        public void SetTextureUnit(GL gl, int t)
        {
            gl.Uniform1(_lGUITexture, t);
        }
    }
}
