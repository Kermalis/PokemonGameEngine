﻿using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.World.Maps;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    // Some movements will break if the obj switches maps because of x/y changes
    internal sealed class EventObj : VisualObj
    {
        private const float NO_MOVEMENT_TIMER = -1f;

        public ObjMovementType MovementType;
        public Vec2I OriginPos;
        public Vec2I MovementRange;
        public TrainerType TrainerType;
        public byte TrainerSight;
        public string Script;
        public Flag Flag;

        /// <summary>True if the player just interacted with this obj and is waiting for it to finish moving to begin the script</summary>
        public bool TalkedTo;
        public override bool CanMoveWillingly => !TalkedTo && base.CanMoveWillingly;
        public override bool ShouldUpdateMovement => TalkedTo || base.ShouldUpdateMovement;

        /// <summary><see cref="NO_MOVEMENT_TIMER"/> means don't update, 0 means update, >0 means wait that many seconds</summary>
        private float _movementTypeTimer;
        private object _movementTypeArg;

        public EventObj(MapEvents.ObjEvent oe, Map map)
            : base(oe.Id, oe.ImageId, oe.Pos)
        {
            MovementType = oe.MovementType;
            InitMovementType();
            OriginPos = oe.Pos.XY;
            MovementRange = oe.MovementRange;
            TrainerType = oe.TrainerType;
            TrainerSight = oe.TrainerSight;
            Script = oe.Script;
            Flag = oe.Flag;
            Map = map;
        }

        // Do not implement "IsSurfing()" because we do not care about "OnDismountFromWater()"
        protected override bool CanSurf()
        {
            return Overworld.IsSurfable(GetBlock().BlocksetBlock.Behavior); // Can only walk to other water blocks if we're on one
        }

        private static readonly FacingDirection[] _allDirections = new FacingDirection[8] { FacingDirection.South, FacingDirection.North, FacingDirection.West, FacingDirection.East,
            FacingDirection.Southwest, FacingDirection.Southeast, FacingDirection.Northwest, FacingDirection.Northeast };
        private static readonly FacingDirection[] _southNorthDirections = new FacingDirection[2] { FacingDirection.South, FacingDirection.North };
        private static readonly FacingDirection[] _westEastDirections = new FacingDirection[2] { FacingDirection.West, FacingDirection.East };
        private void InitMovementType()
        {
            switch (MovementType)
            {
                case ObjMovementType.Face_South: Facing = FacingDirection.South; _movementTypeTimer = NO_MOVEMENT_TIMER; break;
                case ObjMovementType.Face_Southwest: Facing = FacingDirection.Southwest; _movementTypeTimer = NO_MOVEMENT_TIMER; break;
                case ObjMovementType.Face_Southeast: Facing = FacingDirection.Southeast; _movementTypeTimer = NO_MOVEMENT_TIMER; break;
                case ObjMovementType.Face_North: Facing = FacingDirection.North; _movementTypeTimer = NO_MOVEMENT_TIMER; break;
                case ObjMovementType.Face_Northwest: Facing = FacingDirection.Northwest; _movementTypeTimer = NO_MOVEMENT_TIMER; break;
                case ObjMovementType.Face_Northeast: Facing = FacingDirection.Northeast; _movementTypeTimer = NO_MOVEMENT_TIMER; break;
                case ObjMovementType.Face_West: Facing = FacingDirection.West; _movementTypeTimer = NO_MOVEMENT_TIMER; break;
                case ObjMovementType.Face_East: Facing = FacingDirection.East; _movementTypeTimer = NO_MOVEMENT_TIMER; break;
                case ObjMovementType.Face_Randomly:
                case ObjMovementType.Wander_Randomly: Facing = GetRandomDirection(); _movementTypeTimer = GetRandomTimer(); break;
                case ObjMovementType.Wander_SouthAndNorth: Facing = GetRandomDirection(FacingDirection.South, FacingDirection.North); _movementTypeTimer = GetRandomTimer(); break;
                case ObjMovementType.Wander_WestAndEast: Facing = GetRandomDirection(FacingDirection.West, FacingDirection.East); _movementTypeTimer = GetRandomTimer(); break;
                case ObjMovementType.Walk_WestThenReturn: Facing = FacingDirection.West; MovementProgress = 0f; _movementTypeArg = false; break;
                case ObjMovementType.Walk_EastThenReturn: Facing = FacingDirection.East; MovementProgress = 0f; _movementTypeArg = false; break;
            }
        }

        private static FacingDirection GetRandomDirection()
        {
            return (FacingDirection)PBEDataProvider.GlobalRandom.RandomInt(0, 7); // 8 directions
        }
        private static FacingDirection GetRandomDirection(IReadOnlyList<FacingDirection> dirs)
        {
            return PBEDataProvider.GlobalRandom.RandomElement(dirs);
        }
        private static FacingDirection GetRandomDirection(FacingDirection a, FacingDirection b)
        {
            return PBEDataProvider.GlobalRandom.RandomBool() ? a : b;
        }
        private static int GetRandomTimer()
        {
            return PBEDataProvider.GlobalRandom.RandomInt(1, 10);
        }

        private void WanderSomewhere(FacingDirection[] allowed)
        {
            // 1/4 chance to face a direction without moving
            if (PBEDataProvider.GlobalRandom.RandomBool(1, 4))
            {
                goto justFace;
            }
            bool allowSurf = CanSurf();
            // Check if we are in range of our movement radius
            Vec2I p = Pos.XY;
            bool south = Math.Abs(p.Y + 1 - OriginPos.Y) <= MovementRange.Y;
            bool north = Math.Abs(p.Y - 1 - OriginPos.Y) <= MovementRange.Y;
            bool west = Math.Abs(p.X - 1 - OriginPos.X) <= MovementRange.X;
            bool east = Math.Abs(p.X + 1 - OriginPos.X) <= MovementRange.X;
            // Filter out places we cannot go
            var list = new List<FacingDirection>(allowed);
            if (list.Contains(FacingDirection.South) && (!south || !IsMovementLegal(FacingDirection.South, allowSurf)))
            {
                list.Remove(FacingDirection.South);
            }
            if (list.Contains(FacingDirection.Southwest) && (!south || !west || !IsMovementLegal(FacingDirection.Southwest, allowSurf)))
            {
                list.Remove(FacingDirection.Southwest);
            }
            if (list.Contains(FacingDirection.Southeast) && (!south || !east || !IsMovementLegal(FacingDirection.Southeast, allowSurf)))
            {
                list.Remove(FacingDirection.Southeast);
            }
            if (list.Contains(FacingDirection.North) && (!north || !IsMovementLegal(FacingDirection.North, allowSurf)))
            {
                list.Remove(FacingDirection.North);
            }
            if (list.Contains(FacingDirection.Northwest) && (!north || !west || !IsMovementLegal(FacingDirection.Northwest, allowSurf)))
            {
                list.Remove(FacingDirection.Northwest);
            }
            if (list.Contains(FacingDirection.Northeast) && (!north || !east || !IsMovementLegal(FacingDirection.Northeast, allowSurf)))
            {
                list.Remove(FacingDirection.Northeast);
            }
            if (list.Contains(FacingDirection.West) && (!west || !IsMovementLegal(FacingDirection.West, allowSurf)))
            {
                list.Remove(FacingDirection.West);
            }
            if (list.Contains(FacingDirection.East) && (!east || !IsMovementLegal(FacingDirection.East, allowSurf)))
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
                if (Pos.XY.X + MovementRange.X <= OriginPos.X)
                {
                    _movementTypeArg = true;
                }
            }
            else
            {
                Move(FacingDirection.East, false, false);
                if (Pos.XY.X == OriginPos.X)
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
                if (Pos.XY.X - MovementRange.X >= OriginPos.X)
                {
                    _movementTypeArg = true;
                }
            }
            else
            {
                Move(FacingDirection.West, false, false);
                if (Pos.XY.X == OriginPos.X)
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
            _movementTypeTimer = 1.75f;
        }

        public override void Update()
        {
            if (!CanMoveWillingly || MovementType == ObjMovementType.None)
            {
                return; // Cannot move
            }
            float curTimer = _movementTypeTimer;
            if (curTimer == NO_MOVEMENT_TIMER)
            {
                return; // No movement
            }
            // Deduct timer
            _movementTypeTimer = curTimer - Display.DeltaTime;
            if (_movementTypeTimer > 0)
            {
                return; // Not ready
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
