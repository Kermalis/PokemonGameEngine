using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Fonts
{
    internal sealed class FontShader : Shader2D
    {
        private const string VERTEX_SHADER_PATH = @"Shaders\Font.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"Shaders\Font.frag.glsl";

        public static FontShader Instance { get; private set; } = null!; // Initialized in RenderManager

        private readonly int _lFontTexture;
        private readonly int _lRelPos;
        private readonly int _lNumFontColors;
        private readonly int[] _lFontColors;

        public FontShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            Instance = this;

            _lFontTexture = GetUniformLocation(gl, "fontTexture");
            _lRelPos = GetUniformLocation(gl, "relPos");
            _lNumFontColors = GetUniformLocation(gl, "numFontColors");
            _lFontColors = new int[256];
            for (int i = 0; i < 256; i++)
            {
                _lFontColors[i] = GetUniformLocation(gl, "fontColors[" + i + ']');
            }

            // Set texture unit now
            Use(gl);
            gl.Uniform1(_lFontTexture, 0);
        }

        public void SetTranslation(GL gl, ref Pos2D t)
        {
            gl.Uniform2(_lRelPos, t.X, t.Y);
        }
        public void SetColors(GL gl, Vector4[] colors)
        {
            gl.Uniform1(_lNumFontColors, (uint)colors.Length);
            for (int i = 0; i < colors.Length; i++)
            {
                Colors.PutInShader(gl, _lFontColors[i], colors[i]);
            }
        }
    }
}
