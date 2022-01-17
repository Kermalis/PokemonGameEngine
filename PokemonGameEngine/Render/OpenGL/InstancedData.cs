using Silk.NET.OpenGL;
using System;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal sealed class InstancedData
    {
        private readonly uint _vbo;
        public readonly int Capacity;

        public uint InstanceCount { get; private set; }

        public InstancedData(uint vbo, int capacity)
        {
            _vbo = vbo;
            Capacity = capacity;
        }

        public static unsafe uint CreateInstancedVBO(GL gl, nuint size, BufferUsageARB usage)
        {
            uint vbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            gl.BufferData(BufferTargetARB.ArrayBuffer, size, null, usage); // Create empty vbo
            return vbo;
        }
        public static unsafe void AddInstancedAttribute(GL gl, uint attribIndex, int dataSize, uint stride, uint offset)
        {
            gl.EnableVertexAttribArray(attribIndex);
            gl.VertexAttribPointer(attribIndex, dataSize, VertexAttribPointerType.Float, false, stride, (void*)offset);
            gl.VertexAttribDivisor(attribIndex, 1);
        }
        public static void AddInstancedAttribute_Matrix4x4(GL gl, uint firstAttribIndex, uint stride, uint offset)
        {
            AddInstancedAttribute(gl, firstAttribIndex, 4, stride, offset);
            AddInstancedAttribute(gl, firstAttribIndex + 1, 4, stride, offset + (sizeof(float) * 4));
            AddInstancedAttribute(gl, firstAttribIndex + 2, 4, stride, offset + (sizeof(float) * 8));
            AddInstancedAttribute(gl, firstAttribIndex + 3, 4, stride, offset + (sizeof(float) * 12));
        }

        public void Prepare()
        {
            InstanceCount = 0;
        }
        public unsafe void AddInstance(GL gl, void* data, uint dataSize)
        {
            int i = (int)InstanceCount;
            if (i >= Capacity)
            {
                throw new InvalidOperationException("Too many instances being rendered");
            }

            InstanceCount++;
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            gl.BufferSubData(BufferTargetARB.ArrayBuffer, i * (int)dataSize, dataSize, data);
        }

        public void Delete(GL gl)
        {
            gl.DeleteBuffer(_vbo);
        }
    }
}
