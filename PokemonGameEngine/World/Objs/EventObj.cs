using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.UI;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    // Some movements will break if the obj switches maps because of x/y changes
    internal sealed class EventObj : VisualObj
    {
        public ObjMovementType MovementType;
        public int OriginX;
        public int MovementX;
        public int OriginY;
        public int MovementY;
        public TrainerType TrainerType;
        public byte TrainerSight;
        public string Script;
        public Flag Flag;

        private int _movementTypeTimer; // -1 means never run the tick, 0 means run the tick, >=1 means wait that many ticks
        private object _movementTypeArg;

        public EventObj(Map.Events.ObjEvent oe, Map map)
            : base(oe.Id, oe.Sprite, new Position(oe))
        {
            MovementType = oe.MovementType;
            InitMovementType();
            OriginX = oe.X;
            MovementX = oe.MovementX;
            OriginY = oe.Y;
            MovementY = oe.MovementY;
            TrainerType = oe.TrainerType;
            TrainerSight = oe.TrainerSight;
            Script = oe.Script;
            Flag = oe.Flag;
            map.Objs.Add(this);
            Map = map;
        }

        private static readonly FacingDirection[] _allDirections = new FacingDirection[8] { FacingDirection.South, FacingDirection.North, FacingDirection.West, FacingDirection.East,
            FacingDirection.Southwest, FacingDirection.Southeast, FacingDirection.Northwest, FacingDirection.Northeast };
        private static readonly FacingDirection[] _southNorthDirections = new FacingDirection[2] { FacingDirection.South, FacingDirection.North };
        private static readonly FacingDirection[] _westEastDirections = new FacingDirection[2] { FacingDirection.West, FacingDirection.East };
        private void InitMovementType()
        {
            switch (MovementType)
            {
                case ObjMovementType.Face_South: Facing = FacingDirection.South; _movementTypeTimer = -1; break;
                case ObjMovementType.Face_Southwest: Facing = FacingDirection.Southwest; _movementTypeTimer = -1; break;
                case ObjMovementType.Face_Southeast: Facing = FacingDirection.Southeast; _movementTypeTimer = -1; break;
                case ObjMovementType.Face_North: Facing = FacingDirection.North; _movementTypeTimer = -1; break;
                case ObjMovementType.Face_Northwest: Facing = FacingDirection.Northwest; _movementTypeTimer = -1; break;
                case ObjMovementType.Face_Northeast: Facing = FacingDirection.Northeast; _movementTypeTimer = -1; break;
                case ObjMovementType.Face_West: Facing = FacingDirection.West; _movementTypeTimer = -1; break;
                case ObjMovementType.Face_East: Facing = FacingDirection.East; _movementTypeTimer = -1; break;
                case ObjMovementType.Face_Randomly:
                case ObjMovementType.Wander_Randomly: Facing = GetRandomDirection(); _movementTypeTimer = GetRandomTimer(); break;
                case ObjMovementType.Wander_SouthAndNorth: Facing = GetRandomDirection(FacingDirection.South, FacingDirection.North); _movementTypeTimer = GetRandomTimer(); break;
                case ObjMovementType.Wander_WestAndEast: Facing = GetRandomDirection(FacingDirection.West, FacingDirection.East); _movementTypeTimer = GetRandomTimer(); break;
                case ObjMovementType.Walk_WestThenReturn: Facing = FacingDirection.West; MovementTimer = 0; _movementTypeArg = false; break;
                case ObjMovementType.Walk_EastThenReturn: Facing = FacingDirection.East; MovementTimer = 0; _movementTypeArg = false; break;
            }
        }

        private FacingDirection GetRandomDirection()
        {
            return (FacingDirection)PBEDataProvider.GlobalRandom.RandomInt(0, 7); // 8 directions
        }
        private FacingDirection GetRandomDirection(IReadOnlyList<FacingDirection> dirs)
        {
            return PBEDataProvider.GlobalRandom.RandomElement(dirs);
        }
        private FacingDirection GetRandomDirection(FacingDirection a, FacingDirection b)
        {
            return PBEDataProvider.GlobalRandom.RandomBool() ? a : b;
        }
        private int GetRandomTimer()
        {
            return PBEDataProvider.GlobalRandom.RandomInt(1 * Program.NumTicksPerSecond, 10 * Program.NumTicksPerSecond);
        }

        private void WanderSomewhere(FacingDirection[] allowed)
        {
            // 1/4 chance to face a direction without moving
            if (PBEDataProvider.GlobalRandom.RandomBool(1, 4))
            {
                goto justFace;
            }
            // Check if we are in range of our movement radius
            Position p = Pos;
            bool south = Math.Abs(p.Y + 1 - OriginY) <= MovementY;
            bool north = Math.Abs(p.Y - 1 - OriginY) <= MovementY;
            bool west = Math.Abs(p.X - 1 - OriginX) <= MovementX;
            bool east = Math.Abs(p.X + 1 - OriginX) <= MovementX;
            // Filter out places we cannot go
            var list = new List<FacingDirection>(allowed);
            if (list.Contains(FacingDirection.South) && (!south || !IsMovementLegal(FacingDirection.South)))
            {
                list.Remove(FacingDirection.South);
            }
            if (list.Contains(FacingDirection.Southwest) && (!south || !west || !IsMovementLegal(FacingDirection.Southwest)))
            {
                list.Remove(FacingDirection.Southwest);
            }
            if (list.Contains(FacingDirection.Southeast) && (!south || !east || !IsMovementLegal(FacingDirection.Southeast)))
            {
                list.Remove(FacingDirection.Southeast);
            }
            if (list.Contains(FacingDirection.North) && (!north || !IsMovementLegal(FacingDirection.North)))
            {
                list.Remove(FacingDirection.North);
            }
            if (list.Contains(FacingDirection.Northwest) && (!north || !west || !IsMovementLegal(FacingDirection.Northwest)))
            {
                list.Remove(FacingDirection.Northwest);
            }
            if (list.Contains(FacingDirection.Northeast) && (!north || !east || !IsMovementLegal(FacingDirection.Northeast)))
            {
                list.Remove(FacingDirection.Northeast);
            }
            if (list.Contains(FacingDirection.West) && (!west || !IsMovementLegal(FacingDirection.West)))
            {
                list.Remove(FacingDirection.West);
            }
            if (list.Contains(FacingDirection.East) && (!east || !IsMovementLegal(FacingDirection.East)))
            {
                list.Remove(FacingDirection.East);
            }
            // If we can go somewhere
            if (list.Count != 0)
            {
                Move(GetRandomDirection(list), false, true); // We can ignore legal check because we already did it
                return;
            }
        justFace:
            Facing = GetRandomDirection(allowed);
        }
        // Arg is a bool that indicates whether we are returning or not
        private void WalkWestThenReturn()
        {
            // Going west
            if (false.Equals(_movementTypeArg))
            {
                Move(FacingDirection.West, false, false);
                if (Pos.X + MovementX <= OriginX)
                {
                    _movementTypeArg = true;
                }
            }
            else
            {
                Move(FacingDirection.East, false, false);
                if (Pos.X == OriginX)
                {
                    _movementTypeArg = false;
                }
            }
        }
        private void WalkEastThenReturn()
        {
            // Going east
            if (false.Equals(_movementTypeArg))
            {
                Move(FacingDirection.East, false, false);
                if (Pos.X - MovementX >= OriginX)
                {
                    _movementTypeArg = true;
                }
            }
            else
            {
                Move(FacingDirection.West, false, false);
                if (Pos.X == OriginX)
                {
                    _movementTypeArg = false;
                }
            }
        }
        private void SleepMovement()
        {
            if (false.Equals(_movementTypeArg))
            {
                Facing = FacingDirection.North;
                _movementTypeArg = true;
            }
            else
            {
                Facing = FacingDirection.South;
                _movementTypeArg = false;
            }
            _movementTypeTimer = 25;
        }

        public override void LogicTick()
        {
            // Do not run tick if timer is -1 or we cannot move
            if (!CanMoveWillingly)
            {
                return;
            }
            int curTimer = _movementTypeTimer;
            if (curTimer == -1)
            {
                return;
            }
            // Deduct timer
            if (curTimer > 0)
            {
                _movementTypeTimer = curTimer - 1;
                if (curTimer > 1)
                {
                    return;
                }
            }
            // Do movement
            switch (MovementType)
            {
                // Movements with random timers break to go below
                case ObjMovementType.Face_Randomly: Facing = GetRandomDirection(); break;
                case ObjMovementType.Wander_Randomly: WanderSomewhere(_allDirections); break;
                case ObjMovementType.Wander_SouthAndNorth: WanderSomewhere(_southNorthDirections); break;
                case ObjMovementType.Wander_WestAndEast: WanderSomewhere(_westEastDirections); break;
                // Movements without random timers
                case ObjMovementType.Sleep: SleepMovement(); return;
                case ObjMovementType.Walk_WestThenReturn: WalkWestThenReturn(); return;
                case ObjMovementType.Walk_EastThenReturn: WalkEastThenReturn(); return;
            }
            // Movements that didn't "return;" above come down here to set a random timer
            _movementTypeTimer = GetRandomTimer();
        }
    }
}
