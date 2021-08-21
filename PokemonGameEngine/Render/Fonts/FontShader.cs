using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Fonts
{
    internal sealed class FontShader : Shader2D
    {
        private readonly int _lFontTexture;
        private readonly int _lTranslation;
        private readonly int _lNumFontColors;
        private readonly int[] _lFontColors;

        public FontShader(GL gl)
            : base(gl, "Shaders.font_vert.glsl", "Shaders.font_frag.glsl")
        {
            _lFontTexture = GetUniformLocation(gl, "fontTexture");
            _lTranslation = GetUniformLocation(gl, "translation");
            _lNumFontColors = GetUniformLocation(gl, "numFontColors");
            _lFontColors = new int[256];
            for (int i = 0; i < 256; i++)
            {
                _lFontColors[i] = GetUniformLocation(gl, "fontColors[" + i + ']');
            }
        }

        public void SetTextureUnit(GL gl, int t)
        {
            gl.Uniform1(_lFontTexture, t);
        }
        public void SetTranslation(GL gl, ref Pos2D t)
        {
            gl.Uniform2(_lTranslation, t.X, t.Y);
        }
        public void SetColors(GL gl, ColorF[] colors)
        {
            gl.Uniform1(_lNumFontColors, (uint)colors.Length);
            for (int i = 0; i < colors.Length; i++)
            {
                ColorF c = colors[i];
                gl.Uniform4(_lFontColors[i], c.R, c.G, c.B, c.A);
            }
        }
    }
}
