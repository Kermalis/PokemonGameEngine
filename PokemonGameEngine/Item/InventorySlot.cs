using System;
using System.Diagnostics;

namespace Kermalis.PokemonGameEngine.Item
{
    [DebuggerDisplay("Item={" + nameof(Item) + "}, Quantity={" + nameof(Quantity) + "}")]
    internal class InventorySlot
    {
        public const ushort MaxQuantity = 999;

        public ItemType Item;
        public ushort Quantity;

        public InventorySlot(ItemType item, ushort quantity)
        {
            Item = item;
            Quantity = quantity;
        }

        public bool HasQuantity(ushort quantity)
        {
            return Quantity >= quantity;
        }
        public bool HasRoom(ushort quantity)
        {
            return Quantity + quantity <= MaxQuantity;
        }

        public static void ValidateQuantity(ushort quantity)
        {
            if (quantity < 1 || quantity > MaxQuantity)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }
        }
    }
    internal class InventorySlotNew : InventorySlot
    {
        public bool New;

        public InventorySlotNew(ItemType item, ushort quantity)
            : base(item, quantity)
        {
            New = true;
        }
    }
}
