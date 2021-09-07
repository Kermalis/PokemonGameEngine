using Kermalis.PokemonGameEngine.Render;
using System;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal sealed class CameraObj : Obj
    {
        public static readonly CameraObj Camera = new();
        public static Pos2D CameraVisualOffset;
        public static Obj CameraAttachedTo { get; private set; }

        private CameraObj()
            : base(Overworld.CameraId)
        {
        }
        public static void Init()
        {
            CameraAttachedTo = PlayerObj.Player;
            Camera.Pos = PlayerObj.Player.Pos;
            Camera.Map = PlayerObj.Player.Map;
            Camera.Map.Objs.Add(Camera);
        }

        public static void CopyMovementIfAttachedTo(Obj obj)
        {
            if (CameraAttachedTo == obj)
            {
                CameraCopyMovement();
            }
        }
        public static void CameraCopyMovement()
        {
            CameraObj c = Camera;
            Obj other = CameraAttachedTo;
            c.IsMovingSelf = other.IsMovingSelf;
            c.IsScriptMoving = other.IsScriptMoving;
            c.MovementTimer = other.MovementTimer;
            c.MovementSpeed = other.MovementSpeed;
            c.Pos = other.Pos;
            c.VisualOffset = other.VisualOffset;
            c.PrevPos = other.PrevPos;
            c.PrevVisualOffset = other.PrevVisualOffset;
            c.ProgressX = other.ProgressX;
            c.ProgressY = other.ProgressY;
            c.UpdateMap(other.Map);
        }
        public static void SetCameraAttachedTo(Obj o)
        {
            CameraAttachedTo = o;
            if (o is not null)
            {
                CameraCopyMovement();
            }
        }

        public override bool CollidesWithOthers()
        {
            return false;
        }
        protected override bool CanSurf()
        {
            return true;
        }

        public override void Dispose()
        {
            throw new InvalidOperationException();
        }
    }
}
