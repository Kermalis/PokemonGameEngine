namespace Kermalis.PokemonGameEngine.Item
{
    internal enum ItemPouchType : byte
    {
        FreeSpace, // Formerly the pouch in the PC, this is known as "Free space" in gen 5; it accepts all types (except for KeyItems before gen 5)
        Items,
        Medicine,
        KeyItems,
        TMHMs,
        Berries,
        Balls, // This and below are not from gen 5
        Mail,
        BattleItems
    }
}
