using Kermalis.PokemonGameEngine.Item;
using System;
using System.Linq;

namespace Kermalis.PokemonGameEngine.Player
{
    internal sealed class PlayerInventory : Inventory<InventorySlotNew>
    {
        public PlayerInventory()
            : base(Enum.GetValues<ItemPouchType>().Select(t => new InventoryPouch<InventorySlotNew>(t)).ToArray())
        {
            // All pouch types
        }

        public bool HasShinyCharm()
        {
            return this[ItemPouchType.KeyItems][ItemType.ShinyCharm] is not null;
        }
    }
}
