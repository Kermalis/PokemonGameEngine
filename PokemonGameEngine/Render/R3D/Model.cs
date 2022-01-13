using Kermalis.PokemonGameEngine.Render.Shaders.Battle;
using System.Collections.Generic;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal sealed class Model
    {
        private readonly List<Mesh> _meshes;

        public Vector3 Scale;
        public PositionRotation PR;

        public Model(string asset)
        {
            _meshes = AssimpLoader.ImportModel(asset);

            Scale = Vector3.One;
            PR = PositionRotation.Default;
        }

        public Matrix4x4 GetTransformation()
        {
            return Matrix4x4.CreateScale(Scale)
                * Matrix4x4.CreateFromQuaternion(PR.Rotation.Value)
                * Matrix4x4.CreateTranslation(PR.Position);
        }

        public void Render(BattleModelShader shader)
        {
            for (int i = 0; i < _meshes.Count; i++)
            {
                _meshes[i].Render(shader);
            }
        }

        public void Delete()
        {
            for (int i = 0; i < _meshes.Count; i++)
            {
                _meshes[i].Delete();
            }
        }
    }
}
