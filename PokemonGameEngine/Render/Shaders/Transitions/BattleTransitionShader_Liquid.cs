using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Shaders.Transitions
{
    internal sealed class BattleTransitionShader_Liquid : GLShader
    {
        private const string VERTEX_SHADER_PATH = @"EntireScreenTexture.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"Transitions\BattleTransition_Liquid.frag.glsl";

        public static BattleTransitionShader_Liquid Instance { get; private set; } = null!; // Initialized in RenderManager

        private readonly int _lProgress;

        public BattleTransitionShader_Liquid(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            Instance = this;

            _lProgress = GetUniformLocation(gl, "progress");

            // Set texture unit now
            Use(gl);
            gl.Uniform1(GetUniformLocation(gl, "colorTexture"), 0);
        }

        public void SetProgress(GL gl, float progress)
        {
            gl.Uniform1(_lProgress, progress);
        }
    }
}
