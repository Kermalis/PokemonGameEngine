using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal struct TexVertex
    {
        public const int OffsetOfPos = 0;
        public const int OffsetOfTexCoords = OffsetOfPos + (2 * sizeof(int));
        public const uint SizeOf = OffsetOfTexCoords + (2 * sizeof(float));

        public readonly Pos2D Pos;
        public readonly RelPos2D TexCoords;

        public TexVertex(Pos2D pos, RelPos2D tex)
        {
            Pos = pos;
            TexCoords = tex;
        }
    }

    internal sealed class TexVertexBuilder
    {
        private readonly TexVertex[] _vertices;
        private readonly uint[] _indices;
        private uint _vertexCount;
        private uint _indexCount;

        public TexVertexBuilder(int capacity)
        {
            _vertices = new TexVertex[capacity * 4];
            _indices = new uint[capacity * 6];
        }

        public void Add(in Rect2D pos, in AtlasPos tex)
        {
            uint vIndex = _vertexCount;
            _vertexCount += 4;
            _vertices[vIndex + 0] = new TexVertex(pos.TopLeft, tex.GetTopLeft());
            _vertices[vIndex + 1] = new TexVertex(pos.GetExclusiveBottomLeft(), tex.GetBottomLeft());
            _vertices[vIndex + 2] = new TexVertex(pos.GetExclusiveTopRight(), tex.GetTopRight());
            _vertices[vIndex + 3] = new TexVertex(pos.GetExclusiveBottomRight(), tex.GetBottomRight());
            _indices[_indexCount++] = vIndex + 0;
            _indices[_indexCount++] = vIndex + 1;
            _indices[_indexCount++] = vIndex + 2;
            _indices[_indexCount++] = vIndex + 2;
            _indices[_indexCount++] = vIndex + 1;
            _indices[_indexCount++] = vIndex + 3;
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
            fixed (void* d = _vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, TexVertex.SizeOf * _vertexCount, d, BufferUsageARB.StaticDraw);
            }
            // Store in ebo
            ebo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            fixed (void* d = _indices)
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, sizeof(uint) * indexCount, d, BufferUsageARB.StaticDraw);
            }

            // Now set attribs
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, TexVertex.SizeOf, (void*)TexVertex.OffsetOfPos);
            gl.DisableVertexAttribArray(0);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, TexVertex.SizeOf, (void*)TexVertex.OffsetOfTexCoords);
            gl.DisableVertexAttribArray(1);

            gl.BindVertexArray(0);
        }
    }
}
