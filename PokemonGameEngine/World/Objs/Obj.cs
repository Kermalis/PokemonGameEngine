﻿using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Scripts;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    // Regular movements handled in ObjMovement.cs
    // Script movements handled in Script/ScriptMovement.cs
    internal abstract partial class Obj
    {
        public struct Position : IXYElevation
        {
            public int X { get; set; }
            public int Y { get; set; }
            public byte Elevation { get; set; }
            public int XOffset { get; set; }
            public int YOffset { get; set; }

            public Position(Map.Events.ObjEvent e)
            {
                X = e.X;
                Y = e.Y;
                Elevation = e.Elevation;
                XOffset = 0;
                YOffset = 0;
            }
        }

        public static readonly List<Obj> LoadedObjs = new();

        public readonly ushort Id;

        public FacingDirection Facing;
        public Position Pos;
        public Position PrevPos;
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
            Position p = Pos;
            return Map.GetBlock_InBounds(p.X, p.Y);
        }
        public Map.Layout.Block GetBlockFacing()
        {
            Position p = Pos;
            Overworld.MoveCoords(Facing, p.X, p.Y, out int newX, out int newY);
            return Map.GetBlock_CrossMap(newX, newY, out _, out _, out _);
        }

        public void Warp(IWarp warp)
        {
            var map = Map.LoadOrGet(warp.DestMapId);
            int x = warp.DestX;
            int y = warp.DestY;
            byte e = warp.DestElevation;
            Map.Layout.Block block = map.GetBlock_CrossMap(x, y, out int outX, out int outY, out map); // GetBlock_CrossMap in case our warp is actually in a connection for some reason
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
                    outY--;
                    break;
                }
            }
            UpdateMap(map);
            Pos.X = outX;
            Pos.Y = outY;
            Pos.Elevation = e;
            PrevPos = Pos;
            CameraObj.CopyMovementIfAttachedTo(this);
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
            ushort id = (ushort)Game.Instance.Save.Vars[Var.LastTalked];
            if (id != Overworld.PlayerId)
            {
                Obj looker = GetObj(id);
                looker.LookTowards(PlayerObj.Player);
            }
        }
    }
}
