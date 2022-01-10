using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders.World
{
    internal sealed class DayTintShader : GLShader
    {
        private const string VERTEX_SHADER_PATH = @"EntireScreenTexture.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"World\DayTint.frag.glsl";

        public static DayTintShader Instance { get; private set; } = null!; // Initialized in RenderManager

        private readonly int _lModification;

        public DayTintShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            Instance = this;

            _lModification = GetUniformLocation(gl, "modification");

            // Set texture unit now
            Use(gl);
            gl.Uniform1(GetUniformLocation(gl, "colorTexture"), 0);
        }

        public void SetModification(GL gl, ref Vector3 mod)
        {
            gl.Uniform3(_lModification, ref mod);
        }
    }
}
