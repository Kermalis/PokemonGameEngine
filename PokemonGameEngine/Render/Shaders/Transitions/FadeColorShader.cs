using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders.Transitions
{
    internal sealed class FadeColorShader : GLShader
    {
        private const string VERTEX_SHADER_PATH = @"EntireScreen.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"Transitions\FadeColor.frag.glsl";

        public static FadeColorShader Instance { get; private set; } = null!; // Initialized in RenderManager

        private readonly int _lColor;
        private readonly int _lProgress;

        public FadeColorShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            Instance = this;

            _lColor = GetUniformLocation(gl, "u_color");
            _lProgress = GetUniformLocation(gl, "u_progress");
        }

        public void SetColor(GL gl, in Vector3 color)
        {
            gl.Uniform3(_lColor, color);
        }
        public void SetProgress(GL gl, float progress)
        {
            gl.Uniform1(_lProgress, progress);
        }
    }
}
