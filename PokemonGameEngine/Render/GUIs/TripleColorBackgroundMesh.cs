using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    internal sealed class TripleColorBackgroundMesh
    {
        private struct VertexData
        {
            public const int OffsetOfPos = 0;
            public const int OffsetOfColor = 2 * sizeof(float);
            public const uint SizeOf = OffsetOfColor + sizeof(int);

            public readonly Vector2 Pos;
            public readonly int Color;

            public VertexData(Vector2 pos, int color)
            {
                Pos = pos;
                Color = color;
            }
        }

        public static TripleColorBackgroundMesh Instance { get; private set; } = null!; // Set in RenderManager

        private const int NUM_VERTICES = 7;
        private const float SECOND_COLOR_WIDTH = 0.75f;
        private const float THIRD_COLOR_BEGIN_TOP = 0.25f; // X coords
        private const float THIRD_COLOR_BEGIN_BOTTOM = -0.4f;

        private readonly uint _vao;
        private readonly uint _vbo;

        public unsafe TripleColorBackgroundMesh(GL gl)
        {
            Instance = this;

            // Create vao
            _vao = gl.GenVertexArray();
            gl.BindVertexArray(_vao);

            // Create vbo
            _vbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            fixed (void* vertices = CreateVertices())
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)sizeof(VertexData) * NUM_VERTICES, vertices, BufferUsageARB.StaticDraw);
            }

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, VertexData.SizeOf, (void*)VertexData.OffsetOfPos);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, VertexData.SizeOf, (void*)VertexData.OffsetOfColor);
        }
        private static VertexData[] CreateVertices()
        {
            // GL Coordinates
            return new VertexData[NUM_VERTICES]
            {
                new(new Vector2(                                          -1f,  1f), 0), // 0
                new(new Vector2(THIRD_COLOR_BEGIN_BOTTOM - SECOND_COLOR_WIDTH, -1f), 0), // 1 (offset from 3)
                new(new Vector2(   THIRD_COLOR_BEGIN_TOP - SECOND_COLOR_WIDTH,  1f), 0), // 2 (offset from 4)
                new(new Vector2(                     THIRD_COLOR_BEGIN_BOTTOM, -1f), 1), // 3
                new(new Vector2(                        THIRD_COLOR_BEGIN_TOP,  1f), 1), // 4
                new(new Vector2(                                           1f, -1f), 2), // 5
                new(new Vector2(                                           1f,  1f), 2)  // 6
            };
        }

        public void Render(GL gl)
        {
            gl.ProvokingVertex(VertexProvokingMode.LastVertexConvention);
            gl.BindVertexArray(_vao);
            gl.DrawArrays(PrimitiveType.TriangleStrip, 0, NUM_VERTICES);
            gl.ProvokingVertex(VertexProvokingMode.FirstVertexConvention);
        }
    }
}
