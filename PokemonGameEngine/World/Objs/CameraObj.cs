using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.World.Maps;
using System;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal sealed class CameraObj : Obj
    {
        public static CameraObj Instance { get; private set; } = null!; // Set in Init()

        public Pos2D CamVisualOfs;
        public Obj CamAttachedTo { get; private set; }

        private CameraObj()
            : base(Overworld.CameraId)
        {
        }
        public static void Init()
        {
            Instance = new CameraObj();
            Instance.CamAttachedTo = PlayerObj.Instance;
            Instance.Pos = PlayerObj.Instance.Pos;
            Instance.Map = PlayerObj.Instance.Map;
            Instance.Map.Objs.Add(Instance);
            Instance.Map.OnCurrentMap();
            Overworld.UpdateDayTint();
        }

        public void CopyMovementIfAttachedTo(Obj obj)
        {
            if (CamAttachedTo.Id == obj.Id)
            {
                CopyAttachedToMovement();
            }
        }
        // TODO: (#69) Causes shaking because Obj.UpdateMovement() is also called on the camera as well as the obj it's following, causing the camera to get ahead
        // Will happen in scripts if the player moves and the camera is on the player
        public void CopyAttachedToMovement()
        {
            Obj other = CamAttachedTo;

            SetMap(other.Map);

            Pos = other.Pos;
            VisualOfs = other.VisualOfs;
            MovingFromPos = other.MovingFromPos;
            MovingFromVisualOfs = other.MovingFromVisualOfs;

            IsMovingSelf = other.IsMovingSelf;
            IsScriptMoving = other.IsScriptMoving;
            MovementTimer = other.MovementTimer;
            MovementSpeed = other.MovementSpeed;
            VisualProgress = other.VisualProgress;
        }
        public void SetAttachedToThenCopyMovement(Obj o)
        {
            CamAttachedTo = o;
            if (o is not null)
            {
                CopyAttachedToMovement();
            }
        }

        protected override void OnMapChanged(Map oldMap, Map newMap)
        {
            Overworld.OnCameraMapChanged(oldMap, newMap);
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
