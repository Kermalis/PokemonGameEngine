using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    /// <summary>A rect mesh that takes up the entire screen</summary>
    internal sealed class SimpleRectMesh
    {
        public static SimpleRectMesh Instance { get; private set; } = null!; // Set in RenderManager

        private static readonly Vector2[] _vertices = new Vector2[4]
        {
            new Vector2(-1,  1), // Top Left
            new Vector2(-1, -1), // Bottom Left
            new Vector2( 1,  1), // Top Right
            new Vector2( 1, -1)  // Bottom Right
        };

        private readonly uint _vao;
        private readonly uint _vbo;

        public unsafe SimpleRectMesh(GL gl)
        {
            Instance = this;

            // Create vao
            _vao = gl.GenVertexArray();
            gl.BindVertexArray(_vao);

            // Create vbo
            _vbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            fixed (void* d = _vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)sizeof(Vector2) * 4, d, BufferUsageARB.StaticDraw);
            }

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vector2), null);
        }

        public void Render()
        {
            GL gl = Display.OpenGL;
            gl.BindVertexArray(_vao);
            gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }
    }
}
