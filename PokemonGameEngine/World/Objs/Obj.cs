using Kermalis.PokemonGameEngine.Scripts;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    // Regular movements handled in ObjMovement.cs
    // Script movements handled in Script/ScriptMovement.cs
    internal abstract partial class Obj
    {
        public struct Position
        {
            public int X;
            public int Y;
            public byte Elevation;
            public int XOffset;
            public int YOffset;

            public Position(Map.Events.ObjEvent e)
            {
                X = e.X;
                Y = e.Y;
                Elevation = e.Elevation;
                XOffset = 0;
                YOffset = 0;
            }
        }

        public static readonly List<Obj> LoadedObjs = new List<Obj>(); // Only used by GetObj()

        public readonly ushort Id;

        public FacingDirection Facing;
        public Position Pos;
        public Position PrevPos;
        public Map Map;

        public bool CanMoveWillingly => !IsLocked && !IsMoving;
        public bool IsLocked = false;
        public bool IsMoving = false;
        public bool IsScriptMoving = false;
        public float MovementTimer = 1;
        public float MovementSpeed;
        public int ProgressX;
        public int ProgressY;
        protected const float FaceMovementSpeed = 1 / 3f;
        protected const float NormalMovementSpeed = 1 / 6f;
        protected const float RunningMovementSpeed = 1 / 4f;
        protected const float DiagonalMovementSpeedModifier = 0.7071067811865475f; // (2 / (sqrt((2^2) + (2^2)))
        protected const float BlockedMovementSpeedModifier = 0.8f;
        protected const int StairYOffset = 6; // Any offset will work

        protected Obj(ushort id)
        {
            Id = id;
            Pos = new Position();
            PrevPos = new Position();
            LoadedObjs.Add(this);
        }
        protected Obj(ushort id, Position pos)
        {
            Id = id;
            PrevPos = Pos = pos;
            LoadedObjs.Add(this);
        }

        public static Obj GetObj(ushort id)
        {
            foreach (Obj o in LoadedObjs)
            {
                if (o.Id == id)
                {
                    return o;
                }
            }
            return null;
        }

        public Map.Layout.Block GetBlock()
        {
            return GetBlock(out _);
        }
        public Map.Layout.Block GetBlock(out Map map)
        {
            Position p = Pos;
            return Map.GetBlock_CrossMap(p.X, p.Y, out map);
        }

        public void Warp(IWarp warp)
        {
            var map = Map.LoadOrGet(warp.DestMapId);
            int x = warp.DestX;
            int y = warp.DestY;
            byte e = warp.DestElevation;
            Map.Layout.Block block = map.GetBlock_CrossMap(x, y, out map); // GetBlock_CrossMap in case our warp is actually in a connection for some reason
            // Facing is of the original direction unless the block behavior says otherwise
            // All QueuedScriptMovements will be run after the warp is complete
            switch (block.BlocksetBlock.Behavior)
            {
                case BlocksetBlockBehavior.Warp_WalkSouthOnExit:
                {
                    Facing = FacingDirection.South;
                    QueuedScriptMovements.Enqueue(ScriptMovement.Walk_S);
                    break;
                }
                case BlocksetBlockBehavior.Warp_NoOccupancy_S:
                {
                    Facing = FacingDirection.North;
                    y--;
                    break;
                }
            }
            UpdateMap(map);
            Pos.X = x;
            Pos.Y = y;
            Pos.Elevation = e;
            PrevPos = Pos;
            if (CameraObj.CameraAttachedTo == this)
            {
                CameraObj.CameraCopyMovement();
            }
        }

        protected virtual void UpdateMap(Map newMap)
        {
            Map curMap = Map;
            if (curMap != newMap)
            {
                curMap.Objs.Remove(this);
                newMap.Objs.Add(this);
                Map = newMap;
            }
        }
        public virtual bool CollidesWithOthers()
        {
            return true;
        }
        public bool CollidesWithAny_InBounds(Map map, int x, int y, byte elevation)
        {
            if (CollidesWithOthers())
            {
                foreach (Obj o in map.GetObjs_InBounds(x, y, elevation, this, true))
                {
                    if (o.CollidesWithOthers())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual void LogicTick()
        {
        }
    }
}
