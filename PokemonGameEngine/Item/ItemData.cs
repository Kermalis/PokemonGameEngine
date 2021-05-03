using Kermalis.PokemonBattleEngine.Data;

namespace Kermalis.PokemonGameEngine.Item
{
    internal sealed class ItemData
    {
        public static ItemPouchType GetPouchType(PBEItem item)
        {
            if (item == (PBEItem)631 || item == (PBEItem)632)
            {
                return ItemPouchType.KeyItems; // TODO: For now, oval and shiny charm are key items
            }
            return ItemPouchType.Items; // TODO
        }
    }
}
