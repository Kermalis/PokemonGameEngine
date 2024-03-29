﻿using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render
{
    internal sealed class RectMesh
    {
        private const int NUM_VERTICES = 4;

        public static RectMesh Instance { get; set; } = null!; // Set in RenderManager

        private readonly uint _vao;
        private readonly uint _vbo;

        public unsafe RectMesh(GL gl)
        {
            // Create vao
            _vao = gl.GenVertexArray();
            gl.BindVertexArray(_vao);

            // Create vbo
            _vbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            fixed (void* vertices = CreateVertices())
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, 2 * sizeof(float) * NUM_VERTICES, vertices, BufferUsageARB.StaticDraw);
            }

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), null);
        }
        private static Vector2[] CreateVertices()
        {
            return new Vector2[NUM_VERTICES]
            {
                new Vector2(0, 0), // Top Left
                new Vector2(0, 1), // Bottom Left
                new Vector2(1, 0), // Top Right
                new Vector2(1, 1)  // Bottom Right
            };
        }

        public void Render(GL gl)
        {
            gl.BindVertexArray(_vao);
            gl.DrawArrays(PrimitiveType.TriangleStrip, 0, NUM_VERTICES);
        }
        public void RenderInstanced(GL gl, uint instanceCount)
        {
            gl.BindVertexArray(_vao);
            gl.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, NUM_VERTICES, instanceCount);
        }
        public void RenderInstancedBaseInstance(GL gl, uint first, uint instanceCount)
        {
            gl.BindVertexArray(_vao);
            gl.DrawArraysInstancedBaseInstance(PrimitiveType.TriangleStrip, 0, NUM_VERTICES, instanceCount, first); // Requires OpenGL 4.2 or above. Not available in OpenGL ES
        }

        public void Delete(GL gl)
        {
            gl.DeleteVertexArray(_vao);
            gl.DeleteBuffer(_vbo);
        }
    }
}
