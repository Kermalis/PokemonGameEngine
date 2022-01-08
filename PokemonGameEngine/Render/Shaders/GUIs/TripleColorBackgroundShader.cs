using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders.GUIs
{
    internal sealed class TripleColorBackgroundShader : GLShader
    {
        private const string VERTEX_SHADER_PATH = @"GUIs\TripleColorBackground.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"GUIs\TripleColorBackground.frag.glsl";

        private readonly int _lColor1;
        private readonly int _lColor2;
        private readonly int _lColor3;

        public TripleColorBackgroundShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            _lColor1 = GetUniformLocation(gl, "colors[0]");
            _lColor2 = GetUniformLocation(gl, "colors[1]");
            _lColor3 = GetUniformLocation(gl, "colors[2]");
        }

        public void SetColors(GL gl, in Vector3 color1, in Vector3 color2, in Vector3 color3)
        {
            Colors.PutInShader(gl, _lColor1, color1);
            Colors.PutInShader(gl, _lColor2, color2);
            Colors.PutInShader(gl, _lColor3, color3);
        }
    }
}
