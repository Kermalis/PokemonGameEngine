using System;

namespace Kermalis.PokemonGameEngine.Overworld
{
    [Flags]
    public enum LayoutBlockPassage : byte
    {
        None = 0,
        SouthwestPassage = 1,
        SoutheastPassage = 2,
        NorthwestPassage = 4,
        NortheastPassage = 8,
        AllowOccupancy = 16
    }

    [Flags]
    public enum SignInteractionFaces : byte
    {
        None = 0,
        South = 1,
        North = 2,
        West = 4,
        East = 8,
        Southwest = 16,
        Southeast = 32,
        Northwest = 64,
        Northeast = 128
    }

    public enum BlocksetBlockBehavior : byte
    {
        None,
        AllowElevationChange,
        Sign_AutoStartScript,
        Warp_WalkSouthOnExit,
        Warp_Teleport,
        Surf,
        Waterfall,
        Ledge_S,
        Ledge_N,
        Ledge_W,
        Ledge_E,
        Ledge_SW,
        Ledge_SE,
        Ledge_NW,
        Ledge_NE,
        Blocked_S,
        Blocked_N,
        Blocked_W,
        Blocked_E,
        Blocked_SW,
        Blocked_SE,
        Blocked_NW,
        Blocked_NE
    }
}
