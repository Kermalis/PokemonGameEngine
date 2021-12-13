using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal sealed class Camera
    {
        public Matrix4x4 Projection;
        public PositionRotation PR;

        public Camera(in PositionRotation pr, in Matrix4x4 projection)
        {
            PR = pr;
            Projection = projection;
        }

        public Matrix4x4 CreateViewMatrix()
        {
            return CreateViewMatrix(PR);
        }
        public static Matrix4x4 CreateViewMatrix(in PositionRotation pr)
        {
            return CreateViewMatrix(pr.Position, pr.Rotation.Value);
        }
        public static Matrix4x4 CreateViewMatrix(in Vector3 pos, in Quaternion rot)
        {
            // A camera works by moving the entire world in the opposite direction of the camera
            return Matrix4x4.CreateTranslation(Vector3.Negate(pos)) * Matrix4x4.CreateFromQuaternion(Quaternion.Conjugate(rot));
        }
    }
}
