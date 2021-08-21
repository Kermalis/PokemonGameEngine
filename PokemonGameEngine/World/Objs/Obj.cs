using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Scripts;
using Kermalis.PokemonGameEngine.World.Maps;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    // Regular movements handled in ObjMovement.cs
    // Script movements handled in Script/ScriptMovement.cs
    internal abstract partial class Obj : IDisposable
    {
        public static readonly List<Obj> LoadedObjs = new();

        public readonly ushort Id;

        public FacingDirection Facing;
        public WorldPos Pos;
        /// <summary>VisualOffset and PrevVisualOffset are for stairs for example, where the obj is slightly offset from the normal position</summary>
        public Pos2D VisualOffset;
        public WorldPos PrevPos;
        public Pos2D PrevVisualOffset;
        public Map Map;

        public virtual bool CanMoveWillingly => !IsLocked && !IsMoving;
        // Do not move locked Objs unless they're being moved by scripts
        public virtual bool ShouldUpdateMovement => !IsLocked || IsScriptMoving;
        public bool IsMoving => IsScriptMoving || IsMovingSelf;
        public bool IsLocked = false;
        public bool IsMovingSelf = false;
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
            LoadedObjs.Add(this);
        }
        protected Obj(ushort id, WorldPos pos)
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

        public MapLayout.Block GetBlock()
        {
            WorldPos p = Pos;
            return Map.GetBlock_InBounds(p.X, p.Y);
        }
        public MapLayout.Block GetBlockFacing()
        {
            WorldPos p = Pos;
            Overworld.MoveCoords(Facing, p.X, p.Y, out int newX, out int newY);
            return Map.GetBlock_CrossMap(newX, newY, out _, out _, out _);
        }

        public void Warp()
        {
            WarpInProgress wip = WarpInProgress.Current;
            Map map = wip.DestMapLoaded;
            WorldPos pos = wip.Destination.DestPos;
            MapLayout.Block block = map.GetBlock_CrossMap(pos.X, pos.Y, out int outX, out int outY, out map); // GetBlock_CrossMap in case our warp is actually in a connection for some reason
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
                    outY--; // Can put you outside the map but that's the map designer's problem
                    break;
                }
            }
            UpdateMap(map);
            Pos = new WorldPos(outX, outY, pos.Elevation);
            PrevPos = Pos;
            PrevVisualOffset = VisualOffset;
            CameraObj.CopyMovementIfAttachedTo(this);
            WarpInProgress.EndCurrent();
        }

        protected void UpdateMap(Map newMap)
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

        protected virtual void OnPositionVisiblyChanged() { }
        public virtual void LogicTick() { }

        public static void FaceLastTalkedTowardsPlayer()
        {
            ushort id = (ushort)Engine.Instance.Save.Vars[Var.LastTalked];
            if (id != Overworld.PlayerId)
            {
                Obj looker = GetObj(id);
                looker.LookTowards(PlayerObj.Player);
            }
        }

        public virtual void Dispose() { }
    }
}
