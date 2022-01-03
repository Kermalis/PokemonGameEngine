using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal struct TileVertex
    {
        public const int OffsetOfPos = 0;
        public const int OffsetOfTileset = OffsetOfPos + (2 * sizeof(int));
        public const int OffsetOfUV = OffsetOfTileset + sizeof(int);
        public const uint SizeOf = OffsetOfUV + (2 * sizeof(float));

        public readonly Pos2D Pos;
        public readonly int Tileset;
        public readonly Vector2 UV;

        public TileVertex(Pos2D pos, int tileset, Vector2 uv)
        {
            Pos = pos;
            Tileset = tileset;
            UV = uv;
        }
    }

    internal sealed class TileVertexBuilder
    {
        private readonly List<TileVertex> _vertices = new();
        private readonly List<uint> _indices = new();
        private uint _vertexCount;
        private uint _indexCount;

        public void Add(in Rect2D pos, int tileset, in AtlasPos uv)
        {
            uint vIndex = _vertexCount;
            _vertexCount += 4;
            _vertices.Add(new TileVertex(pos.TopLeft, tileset, uv.Start));
            _vertices.Add(new TileVertex(pos.GetExclusiveBottomLeft(), tileset, uv.GetBottomLeft()));
            _vertices.Add(new TileVertex(pos.GetExclusiveTopRight(), tileset, uv.GetTopRight()));
            _vertices.Add(new TileVertex(pos.GetExclusiveBottomRight(), tileset, uv.End));

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
            gl.BufferData(BufferTargetARB.ArrayBuffer, TileVertex.SizeOf * _vertexCount, (ReadOnlySpan<TileVertex>)CollectionsMarshal.AsSpan(_vertices), BufferUsageARB.StaticDraw);

            // Store in ebo
            ebo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, sizeof(uint) * indexCount, (ReadOnlySpan<uint>)CollectionsMarshal.AsSpan(_indices), BufferUsageARB.StaticDraw);

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, TileVertex.SizeOf, (void*)TileVertex.OffsetOfPos);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, TileVertex.SizeOf, (void*)TileVertex.OffsetOfTileset);
            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, TileVertex.SizeOf, (void*)TileVertex.OffsetOfUV);
        }
    }
}