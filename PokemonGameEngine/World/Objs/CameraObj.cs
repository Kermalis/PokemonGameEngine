using Kermalis.PokemonGameEngine.Render.World;
using System;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal class CameraObj : Obj
    {
        public static readonly CameraObj Camera = new();
        public static int CameraOfsX;
        public static int CameraOfsY;
        public static Obj CameraAttachedTo;

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
            MapRenderer.UpdateVisibleMaps();
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
            MapRenderer.UpdateVisibleMaps();
        }
        public static void SetCameraOffset(int xOffset, int yOffset)
        {
            CameraOfsX = xOffset;
            CameraOfsY = yOffset;
            MapRenderer.UpdateVisibleMaps();
        }

        public override bool CollidesWithOthers()
        {
            return false;
        }
        protected override bool CanSurf()
        {
            return true;
        }
        protected override void OnPositionVisiblyChanged()
        {
            MapRenderer.UpdateVisibleMaps();
        }

        public override void Dispose()
        {
            throw new InvalidOperationException();
        }
    }
}
