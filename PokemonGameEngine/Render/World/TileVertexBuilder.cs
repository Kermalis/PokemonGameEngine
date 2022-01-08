using Kermalis.PokemonGameEngine.Render.Shaders.World;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed class TileVertexBuilder
    {
        private readonly List<VBOData_BlocksetBlockTile> _vertices = new();
        private readonly List<uint> _indices = new();
        private uint _vertexCount;
        private uint _indexCount;

        public void Clear()
        {
            _vertices.Clear();
            _indices.Clear();
            _vertexCount = 0;
            _indexCount = 0;
        }

        public void Add(in Rect pos, int tileset, in UV uv)
        {
            uint vIndex = _vertexCount;
            _vertexCount += 4;
            _vertices.Add(new VBOData_BlocksetBlockTile(pos.TopLeft, tileset, uv.Start));
            _vertices.Add(new VBOData_BlocksetBlockTile(pos.GetExclusiveBottomLeft(), tileset, uv.GetBottomLeft()));
            _vertices.Add(new VBOData_BlocksetBlockTile(pos.GetExclusiveTopRight(), tileset, uv.GetTopRight()));
            _vertices.Add(new VBOData_BlocksetBlockTile(pos.GetExclusiveBottomRight(), tileset, uv.End));

            _indexCount += 6;
            _indices.Add(vIndex + 0);
            _indices.Add(vIndex + 1);
            _indices.Add(vIndex + 2);
            _indices.Add(vIndex + 2);
            _indices.Add(vIndex + 1);
            _indices.Add(vIndex + 3);
        }

        public unsafe void Finish(GL gl, out uint indexCount, out uint vao, out uint vbo, out uint ebo)
        {
            indexCount = _indexCount;

            // Create vao
            vao = gl.GenVertexArray();
            gl.BindVertexArray(vao);

            // Store in vbo
            vbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            gl.BufferData(BufferTargetARB.ArrayBuffer, VBOData_BlocksetBlockTile.SizeOf * _vertexCount, (ReadOnlySpan<VBOData_BlocksetBlockTile>)CollectionsMarshal.AsSpan(_vertices), BufferUsageARB.StaticDraw);

            // Store in ebo
            ebo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, sizeof(uint) * indexCount, (ReadOnlySpan<uint>)CollectionsMarshal.AsSpan(_indices), BufferUsageARB.StaticDraw);

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, VBOData_BlocksetBlockTile.SizeOf, (void*)VBOData_BlocksetBlockTile.OffsetOfPos);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, VBOData_BlocksetBlockTile.SizeOf, (void*)VBOData_BlocksetBlockTile.OffsetOfTileset);
            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, VBOData_BlocksetBlockTile.SizeOf, (void*)VBOData_BlocksetBlockTile.OffsetOfUV);
        }
    }
}