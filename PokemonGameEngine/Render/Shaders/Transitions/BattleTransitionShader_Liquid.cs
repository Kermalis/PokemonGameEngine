using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Shaders.Transitions
{
    internal sealed class BattleTransitionShader_Liquid : GLShader
    {
        private const string VERTEX_SHADER_PATH = @"EntireScreenTexture.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"Transitions\BattleTransition_Liquid.frag.glsl";

        public static BattleTransitionShader_Liquid Instance { get; private set; } = null!; // Initialized in RenderManager

        private readonly int _lProgress;
        private readonly int _lColorTexture;

        public BattleTransitionShader_Liquid(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            Instance = this;

            _lProgress = GetUniformLocation(gl, "progress");
            _lColorTexture = GetUniformLocation(gl, "colorTexture");

            Use(gl);
            gl.Uniform1(_lColorTexture, 0); // Set texture unit now
        }

        public void SetProgress(GL gl, float progress)
        {
            gl.Uniform1(_lProgress, progress);
        }
    }
}
