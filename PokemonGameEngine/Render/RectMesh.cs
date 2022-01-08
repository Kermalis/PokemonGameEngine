﻿using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render
{
    internal sealed class RectMesh
    {
        public static RectMesh Instance { get; private set; } = null!; // Set in RenderManager

        private readonly uint _vao;
        private readonly uint _vbo;

        public unsafe RectMesh(GL gl)
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
                gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)sizeof(Vector2) * 4, vertices, BufferUsageARB.StaticDraw);
            }

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vector2), null);
        }
        private static Vector2[] CreateVertices()
        {
            return new Vector2[4]
            {
                new Vector2(-1,  1), // Top Left
                new Vector2(-1, -1), // Bottom Left
                new Vector2( 1,  1), // Top Right
                new Vector2( 1, -1)  // Bottom Right
            };
        }

        public void Render()
        {
            GL gl = Display.OpenGL;
            gl.BindVertexArray(_vao);
            gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }
    }
}