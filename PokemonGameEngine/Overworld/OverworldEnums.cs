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
        SouthwestPassage = 1 << 0,
        SoutheastPassage = 1 << 1,
        NorthwestPassage = 1 << 2,
        NortheastPassage = 1 << 3,
        AllowOccupancy = 1 << 4
    }

    [Flags]
    public enum SignInteractionFaces : byte
    {
        None = 0,
        South = 1 << 0,
        North = 1 << 1,
        West = 1 << 2,
        East = 1 << 3,
        Southwest = 1 << 4,
        Southeast = 1 << 5,
        Northwest = 1 << 6,
        Northeast = 1 << 7
    }

    [Flags]
    public enum MapFlags : byte
    {
        None = 0,
        DayTint = 1 << 0,
        Bike = 1 << 1,
        Fly = 1 << 2,
        Teleport = 1 << 3,
        Dig_EscapeRope = 1 << 4,
        ShowMapName = 1 << 5
    }

    public enum MapWeather : byte
    {
        None,
        Normal,
        Rain_Light,
        Rain_Medium,
        Sandstorm,
        Snow_Light,
        Snow_Hail,
        Drought,
        Fog_Light,
        Fog_Thick
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

    // These are the sections that define the map name and map location on the world map
    public enum MapSection : byte
    {
        None,
        TestMapC,
        TestMapW,
        TestCave
    }

    public enum BlocksetBlockBehavior : byte
    {
        None,
        AllowElevationChange,
        Sign_AutoStartScript,
        Warp_WalkSouthOnExit,
        Warp_Teleport,
        Grass_Encounter,
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
        Grass_SpecialEncounter,
        Tree_Headbutt,
        Tree_Honey,
        Stair_W,
        Stair_E,
        Warp_NoOccupancy_S,
        Cave_Encounter,
        AllowElevationChange_Cave_Encounter,
        Bridge,
        Bridge_Cave_Encounter
    }

    // TODO
    public enum Song : ushort
    {
        None,
    }
}
