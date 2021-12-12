using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    internal sealed class GUIQuadShader : Shader2D
    {
        private const string VERTEX_SHADER_PATH = @"Shaders\GUI_Quad.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"Shaders\GUI_Quad.frag.glsl";

        private readonly int _lColor;

        public GUIQuadShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            _lColor = GetUniformLocation(gl, "color");
        }

        public void SetColor(GL gl, in Vector4 c)
        {
            Colors.PutInShader(gl, _lColor, c);
        }
    }
}
