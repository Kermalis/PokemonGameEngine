using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.World;
using System;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal sealed class CameraObj : Obj
    {
        public static readonly CameraObj Instance = new();
        public static Pos2D CameraVisualOffset;
        public static Obj CameraAttachedTo { get; private set; }

        private CameraObj()
            : base(Overworld.CameraId)
        {
        }
        public static void Init()
        {
            CameraAttachedTo = PlayerObj.Instance;
            Instance.Pos = PlayerObj.Instance.Pos;
            Instance.Map = PlayerObj.Instance.Map;
            Instance.Map.Objs.Add(Instance);
            Instance.ToggleDayTint();
        }

        public static void CopyMovementIfAttachedTo(Obj obj)
        {
            if (CameraAttachedTo == obj)
            {
                CameraCopyMovement();
            }
        }
        // TODO: (#69) Causes shaking because Obj.UpdateMovement() is also called on the camera as well as the obj it's following, causing the camera to get ahead
        // Will happen in scripts if the player moves and the camera is on the player
        private static void CameraCopyMovement()
        {
            CameraObj c = Instance;
            Obj other = CameraAttachedTo;

            c.UpdateMap(other.Map);

            c.Pos = other.Pos;
            c.VisualOffset = other.VisualOffset;
            c.PrevPos = other.PrevPos;
            c.PrevVisualOffset = other.PrevVisualOffset;

            c.IsMovingSelf = other.IsMovingSelf;
            c.IsScriptMoving = other.IsScriptMoving;
            c.MovementTimer = other.MovementTimer;
            c.MovementSpeed = other.MovementSpeed;
            c.VisualProgress = other.VisualProgress;
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
        protected override void OnMapChanged()
        {
            ToggleDayTint();
        }
        protected override bool CanSurf()
        {
            return true;
        }

        private void ToggleDayTint()
        {
            DayTint.IsEnabled = Map.Details.Flags.HasFlag(MapFlags.DayTint);
        }

        public override void Dispose()
        {
            throw new InvalidOperationException();
        }
    }
}
