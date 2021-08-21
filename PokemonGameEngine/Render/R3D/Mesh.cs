using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal sealed class Mesh
    {
        private readonly AssimpVertex[] _vertices;
        private readonly uint[] _indices;
        private readonly IReadOnlyList<AssimpTexture> _textures;

        private readonly uint _vao;
        private readonly uint _vbo;
        private readonly uint _ebo;

        public unsafe Mesh(AssimpVertex[] vertices, uint[] indices, IReadOnlyList<AssimpTexture> textures)
        {
            _vertices = vertices;
            _indices = indices;
            _textures = textures;

            GL gl = Game.OpenGL;
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
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, sizeof(uint) * (uint)indices.Length, d, BufferUsageARB.StaticDraw);
            }

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, AssimpVertex.SizeOf, (void*)AssimpVertex.OffsetOfPos);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, AssimpVertex.SizeOf, (void*)AssimpVertex.OffsetOfNormal);
            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, AssimpVertex.SizeOf, (void*)AssimpVertex.OffsetOfUV);

            gl.BindVertexArray(0);
        }

        public unsafe void Draw(GL gl, ModelShader shader)
        {
            // Activate all diffuse textures
            for (int i = 0; i < _textures.Count; i++)
            {
                shader.SetDiffuseTextureUnit(gl, i);
                GLHelper.ActiveTexture(gl, i.ToTextureUnit());
                GLHelper.BindTexture(gl, _textures[i].GLTex);
            }

            // Draw
            gl.BindVertexArray(_vao);
            gl.EnableVertexAttribArray(0);
            gl.EnableVertexAttribArray(1);
            gl.EnableVertexAttribArray(2);

            gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);

            gl.DisableVertexAttribArray(0);
            gl.DisableVertexAttribArray(1);
            gl.DisableVertexAttribArray(2);
            gl.BindVertexArray(0);

            // Deactivate all textures
            for (int i = 0; i < _textures.Count; i++)
            {
                GLHelper.ActiveTexture(gl, i.ToTextureUnit());
                GLHelper.BindTexture(gl, 0);
            }
        }

        public void Delete(GL gl)
        {
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
