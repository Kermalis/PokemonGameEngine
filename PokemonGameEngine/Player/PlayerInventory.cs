using Kermalis.PokemonGameEngine.Item;
using System;

namespace Kermalis.PokemonGameEngine.Player
{
    internal sealed class PlayerInventory : Inventory<InventorySlotNew>
    {
        public PlayerInventory()
            : base(CreatePouches())
        {

        }

        private static InventoryPouch<InventorySlotNew>[] CreatePouches()
        {
            // All pouch types
            ItemPouchType[] all = Enum.GetValues<ItemPouchType>();
            var ret = new InventoryPouch<InventorySlotNew>[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                ret[i] = new InventoryPouch<InventorySlotNew>(all[i]);
            }
            return ret;
        }

        public bool HasShinyCharm()
        {
            return this[ItemPouchType.KeyItems][ItemType.ShinyCharm] is not null;
        }
    }
}
