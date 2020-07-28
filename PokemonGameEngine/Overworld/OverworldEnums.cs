using System;

namespace Kermalis.PokemonGameEngine.Overworld
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

    // More can be added easily but this is all I'll have for testing
    public enum EncounterType : byte
    {
        Default,
        Surf,
        SuperRod,
        DarkGrass,
        RareDefault, // Rustling Grass & Dust Clouds
        RareSurf, // Rippling Water
        RareSuperRod, // Rippling Water
        HeadbuttTree,
        HoneyTree
    }

    public enum BlocksetBlockBehavior : byte
    {
        None,
        AllowElevationChange,
        Sign_AutoStartScript,
        Warp_WalkSouthOnExit,
        Warp_Teleport,
        WildEncounter,
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
        Blocked_NE,
        Spin_S,
        Spin_N,
        Spin_W,
        Spin_E,
        Spin_SW,
        Spin_SE,
        Spin_NW,
        Spin_NE,
        DarkGrass,
        HeadbuttTree,
        HoneyTree,
        Stair_W,
        Stair_E
    }
}
