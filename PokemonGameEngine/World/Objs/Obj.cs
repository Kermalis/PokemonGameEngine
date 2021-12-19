﻿using Kermalis.PokemonGameEngine.Core;
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
        public Map Map;
        public WorldPos Pos;
        /// <summary>VisualOffset and PrevVisualOffset are for stairs for example, where the obj is slightly offset from the normal position</summary>
        public Pos2D VisualOffset;
        public WorldPos PrevPos;
        public Pos2D PrevVisualOffset;

        public virtual bool CanMoveWillingly => !IsLocked && !IsMoving;
        // Do not move locked Objs unless they're being moved by scripts
        public virtual bool ShouldUpdateMovement => !IsLocked || IsScriptMoving;
        public bool IsMoving => IsScriptMoving || IsMovingSelf;

        public bool IsLocked = false;
        public bool IsMovingSelf = false;
        public bool IsScriptMoving = false;
        /// <summary>Goes from 0 to 1 inclusive. 1 indicates the movement has finished</summary>
        public float MovementTimer = 1;
        /// <summary>The amount of seconds it takes <see cref="MovementTimer"/> to go from 0 to 1</summary>
        public float MovementSpeed;
        public Pos2D VisualProgress;

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
            return Map.GetBlock_InBounds(Pos.XY);
        }
        public MapLayout.Block GetBlockFacing()
        {
            return Map.GetBlock_CrossMap(Pos.XY.Move(Facing), out _, out _);
        }

        public void Warp()
        {
            WarpInProgress wip = WarpInProgress.Current;
            Map newMap = wip.DestMapLoaded;
            WorldPos newPos = wip.Destination.DestPos;
            MapLayout.Block block = newMap.GetBlock_InBounds(newPos.XY);

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
                    newPos.XY.Y--;
                    break;
                }
            }

            UpdateMap(newMap);
            Pos = newPos;
            PrevPos = newPos;
            PrevVisualOffset = VisualOffset;
            CameraObj.CopyMovementIfAttachedTo(this); // Update camera map and pos
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
                OnMapChanged();
            }
        }
        protected virtual void OnMapChanged() { }
        public virtual bool CollidesWithOthers()
        {
            return true;
        }
        public bool CollidesWithAny_InBounds(Map map, in WorldPos pos)
        {
            if (CollidesWithOthers())
            {
                foreach (Obj o in map.GetObjs_InBounds(pos, this, true))
                {
                    if (o.CollidesWithOthers())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual void Update() { }

        public static void FaceLastTalkedTowardsPlayer()
        {
            ushort id = (ushort)Game.Instance.Save.Vars[Var.LastTalked];
            if (id != Overworld.PlayerId)
            {
                Obj looker = GetObj(id);
                looker.LookTowards(PlayerObj.Instance);
            }
        }

        public virtual void Dispose() { }
    }
}
