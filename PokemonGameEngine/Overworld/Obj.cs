using Kermalis.PokemonGameEngine.Util;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Overworld
{
    internal sealed class Obj
    {
        public enum FacingDirection : byte
        {
            South,
            North,
            West,
            East,
            Southwest,
            Southeast,
            Northwest,
            Northeast
        }

        public static readonly List<Obj> LoadedObjs = new List<Obj>();

        public const ushort PlayerId = ushort.MaxValue;
        public const ushort CameraId = PlayerId - 1;

        public static readonly Obj Player = new Obj(PlayerId, "TestNPC.png", 32, 32);
        public static readonly Obj Camera = new Obj(CameraId);

        public readonly ushort Id;

        public FacingDirection Facing;
        public int X;
        public int Y;
        public byte Elevation;
        public Map Map;

        public bool CanMove = true; // Not too thought-out, so will probably end up removing when scripting/waterfall/currents/spin tiles etc are implemented
        private float _movementTimer;
        private float _movementSpeed; // TODO: Framerate
        private const float _diagonalMovementSpeedModifier = 0.7071067811865475f; // (2 / (sqrt((2^2) + (2^2)))
        private const float _blockedMovementSpeedModifier = 0.8f;
        private bool _leg;
        private int PrevX;
        private int PrevY;
        public int XOffset;
        public int YOffset;

        public readonly int SpriteWidth;
        public readonly int SpriteHeight;
        private readonly uint[][][] _tempSpriteSheet;

        private Obj(ushort id)
        {
            Id = id;
            LoadedObjs.Add(this);
        }
        public Obj(ushort id, string resource, int spriteWidth, int spriteHeight)
        {
            _tempSpriteSheet = RenderUtil.LoadSpriteSheet(resource, spriteWidth, spriteHeight);
            Id = id;
            SpriteWidth = spriteWidth;
            SpriteHeight = spriteHeight;
            LoadedObjs.Add(this);
        }

        public bool Move(FacingDirection facing, bool run)
        {
            CanMove = false;
            _movementTimer = 1;
            _movementSpeed = run ? 0.25f : 0.1f;
            _leg = !_leg;
            Facing = facing;
            PrevX = X;
            PrevY = Y;
            bool success = false;
            // TODO: Elevations, map crossing, ledges
            Map.Layout.Block b = Map.GetBlock(X, Y);
            BlocksetBlockBehavior b_behavior = b.BlocksetBlock.Behavior;
            switch (facing)
            {
                case FacingDirection.South:
                {
                    if (b_behavior != BlocksetBlockBehavior.Blocked_S)
                    {
                        Map.Layout.Block b_s = Map.GetBlock(X, Y + 1);
                        if ((b_s.Passage & LayoutBlockPassage.AllowOccupancy) != 0)
                        {
                            BlocksetBlockBehavior b_s_behavior = b_s.BlocksetBlock.Behavior;
                            if (b_s_behavior != BlocksetBlockBehavior.Surf && b_s_behavior != BlocksetBlockBehavior.Blocked_N)
                            {
                                success = true;
                                Y++;
                            }
                        }
                    }
                    break;
                }
                case FacingDirection.North:
                {
                    if (b_behavior != BlocksetBlockBehavior.Blocked_N)
                    {
                        Map.Layout.Block b_n = Map.GetBlock(X, Y - 1);
                        if ((b_n.Passage & LayoutBlockPassage.AllowOccupancy) != 0)
                        {
                            BlocksetBlockBehavior b_n_behavior = b_n.BlocksetBlock.Behavior;
                            if (b_n_behavior != BlocksetBlockBehavior.Surf && b_n_behavior != BlocksetBlockBehavior.Blocked_S)
                            {
                                success = true;
                                Y--;
                            }
                        }
                    }
                    break;
                }
                case FacingDirection.West:
                {
                    if (b_behavior != BlocksetBlockBehavior.Blocked_W)
                    {
                        Map.Layout.Block b_w = Map.GetBlock(X - 1, Y);
                        if ((b_w.Passage & LayoutBlockPassage.AllowOccupancy) != 0)
                        {
                            BlocksetBlockBehavior b_w_behavior = b_w.BlocksetBlock.Behavior;
                            if (b_w_behavior != BlocksetBlockBehavior.Surf && b_w_behavior != BlocksetBlockBehavior.Blocked_E)
                            {
                                success = true;
                                X--;
                            }
                        }
                    }
                    break;
                }
                case FacingDirection.East:
                {
                    if (b_behavior != BlocksetBlockBehavior.Blocked_E)
                    {
                        Map.Layout.Block b_e = Map.GetBlock(X + 1, Y);
                        if ((b_e.Passage & LayoutBlockPassage.AllowOccupancy) != 0)
                        {
                            BlocksetBlockBehavior b_e_behavior = b_e.BlocksetBlock.Behavior;
                            if (b_e_behavior != BlocksetBlockBehavior.Surf && b_e_behavior != BlocksetBlockBehavior.Blocked_W)
                            {
                                success = true;
                                X++;
                            }
                        }
                    }
                    break;
                }
                case FacingDirection.Southwest:
                {
                    if (b_behavior != BlocksetBlockBehavior.Blocked_S && b_behavior != BlocksetBlockBehavior.Blocked_W && b_behavior != BlocksetBlockBehavior.Blocked_SW)
                    {
                        Map.Layout.Block b_sw = Map.GetBlock(X - 1, Y + 1);
                        if ((b_sw.Passage & LayoutBlockPassage.AllowOccupancy) != 0)
                        {
                            BlocksetBlockBehavior b_sw_behavior = b_sw.BlocksetBlock.Behavior;
                            if (b_sw_behavior != BlocksetBlockBehavior.Surf && b_sw_behavior != BlocksetBlockBehavior.Blocked_N && b_sw_behavior != BlocksetBlockBehavior.Blocked_E && b_sw_behavior != BlocksetBlockBehavior.Blocked_NE)
                            {
                                Map.Layout.Block b_w = Map.GetBlock(X - 1, Y);
                                if ((b_w.Passage & LayoutBlockPassage.SoutheastPassage) != 0)
                                {
                                    BlocksetBlockBehavior b_w_behavior = b_w.BlocksetBlock.Behavior;
                                    if (b_w_behavior != BlocksetBlockBehavior.Blocked_E && b_w_behavior != BlocksetBlockBehavior.Blocked_S && b_w_behavior != BlocksetBlockBehavior.Blocked_SE)
                                    {
                                        Map.Layout.Block b_s = Map.GetBlock(X, Y + 1);
                                        if ((b_s.Passage & LayoutBlockPassage.NorthwestPassage) != 0)
                                        {
                                            BlocksetBlockBehavior b_s_behavior = b_s.BlocksetBlock.Behavior;
                                            if (b_s_behavior != BlocksetBlockBehavior.Blocked_N && b_s_behavior != BlocksetBlockBehavior.Blocked_W && b_s_behavior != BlocksetBlockBehavior.Blocked_NW)
                                            {
                                                success = true;
                                                X--;
                                                Y++;
                                                _movementSpeed *= _diagonalMovementSpeedModifier;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
                case FacingDirection.Southeast:
                {
                    if (b_behavior != BlocksetBlockBehavior.Blocked_S && b_behavior != BlocksetBlockBehavior.Blocked_E && b_behavior != BlocksetBlockBehavior.Blocked_SE)
                    {
                        Map.Layout.Block b_se = Map.GetBlock(X + 1, Y + 1);
                        if ((b_se.Passage & LayoutBlockPassage.AllowOccupancy) != 0)
                        {
                            BlocksetBlockBehavior b_se_behavior = b_se.BlocksetBlock.Behavior;
                            if (b_se_behavior != BlocksetBlockBehavior.Surf && b_se_behavior != BlocksetBlockBehavior.Blocked_N && b_se_behavior != BlocksetBlockBehavior.Blocked_W && b_se_behavior != BlocksetBlockBehavior.Blocked_NW)
                            {
                                Map.Layout.Block b_e = Map.GetBlock(X + 1, Y);
                                if ((b_e.Passage & LayoutBlockPassage.SouthwestPassage) != 0)
                                {
                                    BlocksetBlockBehavior b_e_behavior = b_e.BlocksetBlock.Behavior;
                                    if (b_e_behavior != BlocksetBlockBehavior.Blocked_W && b_e_behavior != BlocksetBlockBehavior.Blocked_S && b_e_behavior != BlocksetBlockBehavior.Blocked_SW)
                                    {
                                        Map.Layout.Block b_s = Map.GetBlock(X, Y + 1);
                                        if ((b_s.Passage & LayoutBlockPassage.NortheastPassage) != 0)
                                        {
                                            BlocksetBlockBehavior b_s_behavior = b_s.BlocksetBlock.Behavior;
                                            if (b_s_behavior != BlocksetBlockBehavior.Blocked_N && b_s_behavior != BlocksetBlockBehavior.Blocked_E && b_s_behavior != BlocksetBlockBehavior.Blocked_NE)
                                            {
                                                success = true;
                                                X++;
                                                Y++;
                                                _movementSpeed *= _diagonalMovementSpeedModifier;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
                case FacingDirection.Northwest:
                {
                    if (b_behavior != BlocksetBlockBehavior.Blocked_N && b_behavior != BlocksetBlockBehavior.Blocked_W && b_behavior != BlocksetBlockBehavior.Blocked_NW)
                    {
                        Map.Layout.Block b_nw = Map.GetBlock(X - 1, Y - 1);
                        if ((b_nw.Passage & LayoutBlockPassage.AllowOccupancy) != 0)
                        {
                            BlocksetBlockBehavior b_nw_behavior = b_nw.BlocksetBlock.Behavior;
                            if (b_nw_behavior != BlocksetBlockBehavior.Surf && b_nw_behavior != BlocksetBlockBehavior.Blocked_S && b_nw_behavior != BlocksetBlockBehavior.Blocked_E && b_nw_behavior != BlocksetBlockBehavior.Blocked_SE)
                            {
                                Map.Layout.Block b_w = Map.GetBlock(X - 1, Y);
                                if ((b_w.Passage & LayoutBlockPassage.NortheastPassage) != 0)
                                {
                                    BlocksetBlockBehavior b_w_behavior = b_w.BlocksetBlock.Behavior;
                                    if (b_w_behavior != BlocksetBlockBehavior.Blocked_E && b_w_behavior != BlocksetBlockBehavior.Blocked_N && b_w_behavior != BlocksetBlockBehavior.Blocked_NE)
                                    {
                                        Map.Layout.Block b_n = Map.GetBlock(X, Y - 1);
                                        if ((b_n.Passage & LayoutBlockPassage.SouthwestPassage) != 0)
                                        {
                                            BlocksetBlockBehavior b_n_behavior = b_n.BlocksetBlock.Behavior;
                                            if (b_n_behavior != BlocksetBlockBehavior.Blocked_S && b_n_behavior != BlocksetBlockBehavior.Blocked_W && b_n_behavior != BlocksetBlockBehavior.Blocked_SW)
                                            {
                                                success = true;
                                                X--;
                                                Y--;
                                                _movementSpeed *= _diagonalMovementSpeedModifier;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
                case FacingDirection.Northeast:
                {
                    if (b_behavior != BlocksetBlockBehavior.Blocked_N && b_behavior != BlocksetBlockBehavior.Blocked_E && b_behavior != BlocksetBlockBehavior.Blocked_NE)
                    {
                        Map.Layout.Block b_ne = Map.GetBlock(X + 1, Y - 1);
                        if ((b_ne.Passage & LayoutBlockPassage.AllowOccupancy) != 0)
                        {
                            BlocksetBlockBehavior b_ne_behavior = b_ne.BlocksetBlock.Behavior;
                            if (b_ne_behavior != BlocksetBlockBehavior.Surf && b_ne_behavior != BlocksetBlockBehavior.Blocked_S && b_ne_behavior != BlocksetBlockBehavior.Blocked_W && b_ne_behavior != BlocksetBlockBehavior.Blocked_SW)
                            {
                                Map.Layout.Block b_e = Map.GetBlock(X + 1, Y);
                                if ((b_e.Passage & LayoutBlockPassage.NorthwestPassage) != 0)
                                {
                                    BlocksetBlockBehavior b_e_behavior = b_e.BlocksetBlock.Behavior;
                                    if (b_e_behavior != BlocksetBlockBehavior.Blocked_W && b_e_behavior != BlocksetBlockBehavior.Blocked_N && b_e_behavior != BlocksetBlockBehavior.Blocked_NW)
                                    {
                                        Map.Layout.Block b_n = Map.GetBlock(X, Y - 1);
                                        if ((b_n.Passage & LayoutBlockPassage.SoutheastPassage) != 0)
                                        {
                                            BlocksetBlockBehavior b_n_behavior = b_n.BlocksetBlock.Behavior;
                                            if (b_n_behavior != BlocksetBlockBehavior.Blocked_S && b_n_behavior != BlocksetBlockBehavior.Blocked_E && b_n_behavior != BlocksetBlockBehavior.Blocked_SE)
                                            {
                                                success = true;
                                                X++;
                                                Y--;
                                                _movementSpeed *= _diagonalMovementSpeedModifier;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(facing));
            }
            if (!success)
            {
                _movementSpeed *= _blockedMovementSpeedModifier;
            }
            UpdateXYOffsets();
            return success;
        }
        private void UpdateXYOffsets()
        {
            float t = _movementTimer;
            XOffset = (int)(t * (PrevX - X) * 16);
            YOffset = (int)(t * (PrevY - Y) * 16);
        }
        public void UpdateMovementTimer()
        {
            if (_movementTimer > 0)
            {
                _movementTimer -= _movementSpeed;
                if (_movementTimer < 0)
                {
                    _movementTimer = 0;
                    // TODO: Keep going for currents/waterfall/spin tiles
                    CanMove = true;
                }
                UpdateXYOffsets();
            }
        }
        public void CopyXY(Obj other)
        {
            CanMove = other.CanMove;
            _movementTimer = other._movementTimer;
            _movementSpeed = other._movementSpeed;
            _leg = other._leg;
            X = other.X;
            Y = other.Y;
            PrevX = other.PrevX;
            PrevY = other.PrevY;
            XOffset = other.XOffset;
            YOffset = other.YOffset;
        }

        // TODO: Shadows, reflections
        public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
        {
            bool ShowLegs()
            {
                float t = _movementTimer;
                return t != 0 && t <= 0.6f;
            }
            byte f = (byte)Facing;
            int spriteNum = ShowLegs() ? (_leg ? f + 8 : f + 16) : f; // TODO: Fall-back to specific sprites if the target sprite doesn't exist
            RenderUtil.Draw(bmpAddress, bmpWidth, bmpHeight, x, y, _tempSpriteSheet[spriteNum], false, false);
        }
    }
}
