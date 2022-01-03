using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
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
        /// <summary><see cref="VisualOfs"/> and <see cref="MovingFromVisualOfs"/> are for stairs for example, where the obj is slightly offset from the normal position</summary>
        public Pos2D VisualOfs;
        public WorldPos MovingFromPos;
        public Pos2D MovingFromVisualOfs;

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
            MovingFromPos = Pos = pos;
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

        public void SetMap(Map newMap)
        {
            Map curMap = Map;
            if (curMap != newMap)
            {
                curMap.Objs.Remove(this);
                newMap.Objs.Add(this);
                Map = newMap;
                OnMapChanged(curMap, newMap);
            }
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
