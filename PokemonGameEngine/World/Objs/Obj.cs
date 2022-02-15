using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.World.Maps;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    // Regular movements handled in ObjMovement.cs
    // Script movements handled in Script/ScriptMovement.cs
    internal abstract partial class Obj : IConnectedListObject<Obj>
    {
        public static readonly ConnectedList<Obj> LoadedObjs = new(IdSorter);
        public bool IsDead;

        public readonly ushort Id;

        public Obj Next { get; set; }
        public Obj Prev { get; set; }

        public FacingDirection Facing;
        public Map Map;
        public WorldPos Pos;
        /// <summary><see cref="VisualOfs"/> and <see cref="MovingFromVisualOfs"/> are for stairs for example, where the obj is slightly offset from the normal position</summary>
        public Vec2I VisualOfs;
        public WorldPos MovingFromPos;
        public Vec2I MovingFromVisualOfs;

        public virtual bool CanMoveWillingly => !IsLocked && !IsMoving;
        // Do not move locked Objs unless they're being moved by scripts
        public virtual bool ShouldUpdateMovement => !IsLocked || IsScriptMoving;
        public bool IsMoving => IsScriptMoving || IsMovingSelf;

        public bool IsLocked = false;
        public bool IsMovingSelf = false;
        public bool IsScriptMoving = false;
        /// <summary>Goes from 0 to 1 inclusive. 1 indicates the movement has finished</summary>
        public float MovementProgress = 1f;
        /// <summary>The amount of seconds it takes <see cref="MovementProgress"/> to go from 0 to 1</summary>
        public float MovementSpeed;
        public Vec2I VisualProgress;

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
            // Would be more optimized with a dictionary but not too worried about it
            for (Obj o = LoadedObjs.First; o is not null; o = o.Next)
            {
                if (o.Id == id)
                {
                    return o;
                }
            }
            return null;
        }
        public static void SetAllLock(bool locked)
        {
            for (Obj o = LoadedObjs.First; o is not null; o = o.Next)
            {
                o.IsLocked = locked;
            }
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

        private static int IdSorter(Obj o1, Obj o2)
        {
            if (o1.Id > o2.Id)
            {
                return -1;
            }
            if (o1.Id == o2.Id)
            {
                return 0; // Should never happen
            }
            return 1;
        }

        public virtual void Dispose()
        {
            IsDead = true;
        }
    }
}
