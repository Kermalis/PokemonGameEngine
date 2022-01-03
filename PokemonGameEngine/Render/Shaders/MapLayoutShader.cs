using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Shaders
{
    internal sealed class MapLayoutShader : Shader2D
    {
        private const string VERTEX_SHADER_PATH = @"MapLayout.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"MapLayout.frag.glsl";

        private readonly int _lTranslation;

        public MapLayoutShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            _lTranslation = GetUniformLocation(gl, "translation");

            // Set tileset texture uniforms right away
            Use(gl);
            for (int i = 0; i < GLTextureUtils.MAX_ACTIVE_TEXTURES; i++)
            {
                gl.Uniform1(GetUniformLocation(gl, "tilesetTextures[" + i + ']'), i);
            }
        }

        public void SetTranslation(GL gl, Pos2D p)
        {
            gl.Uniform2(_lTranslation, p.X, p.Y);
        }
    }
}
