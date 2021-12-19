using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal sealed class BattleSpriteMesh
    {
        private readonly uint _vao;
        private readonly uint _vbo;

        public unsafe BattleSpriteMesh(GL gl)
        {
            // Create vao
            _vao = gl.GenVertexArray();
            gl.BindVertexArray(_vao);

            // Create vbo
            _vbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            fixed (void* vertices = CreateVertices())
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)sizeof(Vector2) * 4, vertices, BufferUsageARB.StaticDraw);
            }

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vector2), null);
        }
        private static Vector2[] CreateVertices()
        {
            // Center x align, Bottom y align
            return new Vector2[4]
            {
                new Vector2(-.5f, 1), // Top Left
                new Vector2(-.5f, 0), // Bottom Left
                new Vector2( .5f, 1), // Top Right
                new Vector2( .5f, 0)  // Bottom Right
            };
        }

        public void Render()
        {
            GL gl = Display.OpenGL;
            gl.BindVertexArray(_vao);
            gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        public void Delete()
        {
            GL gl = Display.OpenGL;
            gl.DeleteVertexArray(_vao);
            gl.DeleteBuffer(_vbo);
        }
    }
}
