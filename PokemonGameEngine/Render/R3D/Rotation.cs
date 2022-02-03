using Kermalis.PokemonGameEngine.Core;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal readonly struct Rotation
    {
        public static Rotation Default { get; } = new(Quaternion.Identity);

        /// <summary>Yaw degrees, where positive turns your head to the right.</summary>
        public readonly float Yaw;
        /// <summary>Pitch degrees, where positive pitches your head downwards.</summary>
        public readonly float Pitch;
        /// <summary>Roll degrees, where positive rolls your head to the right.</summary>
        public readonly float Roll;
        /// <summary>The final Quaternion to be used in a matrix.</summary>
        public readonly Quaternion Value;

        public Rotation(in Quaternion rot)
        {
            Yaw = -rot.GetYawRadians() * Utils.RadToDeg;
            Pitch = -rot.GetPitchRadians() * Utils.RadToDeg;
            Roll = -rot.GetRollRadians() * Utils.RadToDeg;
            Value = rot;
        }
        public Rotation(float yaw, float pitch, float roll)
        {
            Yaw = yaw;
            Pitch = pitch;
            Roll = roll;
            Value = CreateQuaternion(yaw, pitch, roll);
        }

        public static Quaternion CreateQuaternion(float yaw, float pitch, float roll)
        {
            return Quaternion.CreateFromYawPitchRoll(-yaw * Utils.DegToRad, -pitch * Utils.DegToRad, -roll * Utils.DegToRad);
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("Yaw: {0}\nPitch: {1}\nRoll: {2}", Yaw, Pitch, Roll);
        }
#endif
    }
}
