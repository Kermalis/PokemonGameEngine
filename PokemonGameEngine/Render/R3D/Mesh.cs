using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal sealed class Mesh
    {
        private readonly uint _elementCount;
        private readonly IReadOnlyList<AssimpTexture> _textures;

        private readonly uint _vao;
        private readonly uint _vbo;
        private readonly uint _ebo;

        public unsafe Mesh(AssimpVertex[] vertices, uint[] indices, IReadOnlyList<AssimpTexture> textures)
        {
            _elementCount = (uint)indices.Length;
            _textures = textures;

            GL gl = Display.OpenGL;
            _vao = gl.GenVertexArray();
            gl.BindVertexArray(_vao);

            _vbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            fixed (void* d = vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, AssimpVertex.SizeOf * (uint)vertices.Length, d, BufferUsageARB.StaticDraw);
            }

            _ebo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            fixed (void* d = indices)
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, sizeof(uint) * _elementCount, d, BufferUsageARB.StaticDraw);
            }

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, AssimpVertex.SizeOf, (void*)AssimpVertex.OffsetOfPos);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, AssimpVertex.SizeOf, (void*)AssimpVertex.OffsetOfNormal);
            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, AssimpVertex.SizeOf, (void*)AssimpVertex.OffsetOfUV);
        }

        public unsafe void Render(ModelShader shader)
        {
            GL gl = Display.OpenGL;

            // Bind all diffuse textures
            for (int i = 0; i < _textures.Count; i++)
            {
                if (shader.SetDiffuseTextureUnit(gl, i))
                {
                    gl.ActiveTexture(i.ToTextureUnit());
                    gl.BindTexture(TextureTarget.Texture2D, _textures[i].GLTex);
                }
            }

            gl.BindVertexArray(_vao);
            gl.DrawElements(PrimitiveType.Triangles, _elementCount, DrawElementsType.UnsignedInt, null);
        }

        public void Delete()
        {
            GL gl = Display.OpenGL;
            gl.DeleteVertexArray(_vao);
            gl.DeleteBuffer(_vbo);
            gl.DeleteBuffer(_ebo);
            for (int i = 0; i < _textures.Count; i++)
            {
                gl.DeleteTexture(_textures[i].GLTex);
            }
        }
    }
}
