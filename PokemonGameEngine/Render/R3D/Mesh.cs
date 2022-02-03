using Kermalis.PokemonGameEngine.Render.Shaders.Battle;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal sealed class Mesh
    {
        private readonly uint _elementCount;
        private readonly IReadOnlyList<uint> _textures;

        private readonly uint _vao;
        private readonly uint _vbo;
        private readonly uint _ebo;

        public unsafe Mesh(VBOData_BattleModel[] vertices, List<uint> indices, IReadOnlyList<uint> textures)
        {
            _elementCount = (uint)indices.Count;
            _textures = textures;

            GL gl = Display.OpenGL;
            _vao = gl.GenVertexArray();
            gl.BindVertexArray(_vao);

            _vbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            fixed (void* d = vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, VBOData_BattleModel.SIZE * (uint)vertices.Length, d, BufferUsageARB.StaticDraw);
            }

            _ebo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, sizeof(uint) * _elementCount, (ReadOnlySpan<uint>)CollectionsMarshal.AsSpan(indices), BufferUsageARB.StaticDraw);

            VBOData_BattleModel.AddAttributes(gl);
        }

        public unsafe void Render(BattleModelShader shader)
        {
            GL gl = Display.OpenGL;

            // Bind all diffuse textures
            for (int i = 0; i < _textures.Count; i++)
            {
                int textureUnit = i + 2; // +2 because shadow textures are the first two
                if (shader.SetDiffuseTextureUnit(gl, i, textureUnit))
                {
                    gl.ActiveTexture(TextureUnit.Texture0 + textureUnit);
                    gl.BindTexture(TextureTarget.Texture2D, _textures[i]);
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
                gl.DeleteTexture(_textures[i]);
            }
        }
    }
}
