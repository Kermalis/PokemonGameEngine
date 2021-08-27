using Silk.NET.OpenGL;
using System.Collections.Generic;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal sealed class Model
    {
        private readonly List<Mesh> _meshes;

        public Vector3 Scale = Vector3.One;
        public PositionRotation PR;

        public Model(string asset)
        {
            _meshes = AssimpLoader.ImportModel(asset);
            PR = new PositionRotation();
        }

        public Matrix4x4 GetTransformation()
        {
            return Matrix4x4.CreateFromQuaternion(PR.Rotation) * Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateTranslation(PR.Position);
        }

        public void Draw(GL gl, ModelShader shader)
        {
            for (int i = 0; i < _meshes.Count; i++)
            {
                _meshes[i].Draw(gl, shader);
            }
        }

        public void Delete(GL gl)
        {
            for (int i = 0; i < _meshes.Count; i++)
            {
                _meshes[i].Delete(gl);
            }
        }
    }
}
