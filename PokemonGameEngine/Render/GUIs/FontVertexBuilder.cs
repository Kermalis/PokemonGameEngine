using Kermalis.PokemonGameEngine.Render.Shaders.GUIs;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal sealed class FontVertexBuilder
    {
        private readonly VBOData_FontChar[] _vertices;
        private readonly uint[] _indices;
        private uint _vertexCount;
        private uint _indexCount;

        public FontVertexBuilder(int capacity)
        {
            _vertices = new VBOData_FontChar[capacity * 4];
            _indices = new uint[capacity * 6];
        }

        public void Add(in Rect pos, in UV uv)
        {
            uint vIndex = _vertexCount;
            _vertexCount += 4;
            _vertices[vIndex + 0] = new VBOData_FontChar(pos.TopLeft, uv.Start);
            _vertices[vIndex + 1] = new VBOData_FontChar(pos.GetExclusiveBottomLeft(), uv.GetBottomLeft());
            _vertices[vIndex + 2] = new VBOData_FontChar(pos.GetExclusiveTopRight(), uv.GetTopRight());
            _vertices[vIndex + 3] = new VBOData_FontChar(pos.GetExclusiveBottomRight(), uv.End);
            _indices[_indexCount++] = vIndex + 0;
            _indices[_indexCount++] = vIndex + 1;
            _indices[_indexCount++] = vIndex + 2;
            _indices[_indexCount++] = vIndex + 2;
            _indices[_indexCount++] = vIndex + 1;
            _indices[_indexCount++] = vIndex + 3;
        }

        public unsafe void Finish(out uint vao, out uint vbo, out uint ebo)
        {
            GL gl = Display.OpenGL;
            // Create vao
            vao = gl.GenVertexArray();
            gl.BindVertexArray(vao);

            // Store in vbo
            vbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            fixed (void* data = _vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, VBOData_FontChar.SizeOf * _vertexCount, data, BufferUsageARB.StaticDraw);
            }
            // Store in ebo
            ebo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            fixed (void* data = _indices)
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, sizeof(uint) * _indexCount, data, BufferUsageARB.StaticDraw);
            }

            // Now set attribs
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, VBOData_FontChar.SizeOf, (void*)VBOData_FontChar.OffsetOfPos);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, VBOData_FontChar.SizeOf, (void*)VBOData_FontChar.OffsetOfUV);
        }
    }
}
