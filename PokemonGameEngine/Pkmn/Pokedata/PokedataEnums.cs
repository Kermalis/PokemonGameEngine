namespace Kermalis.PokemonGameEngine.Pkmn.Pokedata
{
    // If you're removing/renaming any EvoMethods or EggGroups, anything you dump with the PokemonDumper will be incorrect.
    // You can change the order around freely though
    public enum EvoMethod : byte
    {
        None, // [no param]
        Friendship_LevelUp, // (param = friendship)
        Friendship_Day_LevelUp, // (param = friendship)
        Friendship_Night_LevelUp, // (param = friendship)
        LevelUp, // (param = level)
        Trade, // [no param]
        Item_Trade, // (param = item)
        ShelmetKarrablast, // [no param]
        Stone, // (param = item)
        ATK_GT_DEF_LevelUp, // (param = level)
        ATK_EE_DEF_LevelUp, // (param = level)
        ATK_LT_DEF_LevelUp, // (param = level)
        Silcoon_LevelUp, // (param = level)
        Cascoon_LevelUp, // (param = level)
        Ninjask_LevelUp, // (param = level)
        Shedinja_LevelUp, // (param = level) // I'm keeping this unused because it's not really necessary. Kept for compatibility with the dumper
        Beauty_LevelUp, // (param = beauty amount) // Unused for now
        Male_Stone, // (param = item)
        Female_Stone, // (param = item)
        Item_Day_LevelUp, // (param = item)
        Item_Night_LevelUp, // (param = item)
        Move_LevelUp, // (param = move)
        PartySpecies_LevelUp, // (param = species)
        Male_LevelUp, // (param = level)
        Female_LevelUp, // (param = level)
        NosepassMagneton_Location_LevelUp, // [no param]
        Leafeon_Location_LevelUp, // [no param]
        Glaceon_Location_LevelUp, // [no param]
        MAX
    }
    public enum EggGroup : byte
    {
        Monster,
        Water1,
        Bug,
        Flying,
        Field,
        Fairy,
        Grass,
        HumanLike,
        Water3,
        Mineral,
        Amorphous,
        Water2,
        Ditto,
        Dragon,
        Undiscovered,
        MAX
    }
}
