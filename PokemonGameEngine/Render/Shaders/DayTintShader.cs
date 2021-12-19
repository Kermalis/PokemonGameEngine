using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders
{
    internal sealed class DayTintShader : GLShader
    {
        private const string VERTEX_SHADER_PATH = @"EntireScreen.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"DayTint.frag.glsl";

        public static DayTintShader Instance { get; private set; } = null!; // Initialized in RenderManager

        private readonly int _lModification;
        private readonly int _lColorTexture;

        public DayTintShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            Instance = this;

            _lModification = GetUniformLocation(gl, "modification");
            _lColorTexture = GetUniformLocation(gl, "colorTexture");

            Use(gl);
            gl.Uniform1(_lColorTexture, 0); // Set texture unit now
        }

        public void SetModification(GL gl, ref Vector3 mod)
        {
            gl.Uniform3(_lModification, ref mod);
        }
    }
}
