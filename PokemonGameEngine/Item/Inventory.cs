using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Kermalis.PokemonGameEngine.Item
{
    // In C# 8.0, we can have an interface define a "Create" method which will reduce the need of having "Inventory" and "PlayerInventory" separate
    [DebuggerDisplay("Item={" + nameof(Item) + "}, Quantity={" + nameof(Quantity) + "}")]
    internal class InventorySlot
    {
        public const ushort MaxQuantity = 999;

        public PBEItem Item;
        public ushort Quantity;

        public InventorySlot(PBEItem item, ushort quantity)
        {
            Item = item;
            Quantity = quantity;
        }

        public void Add(ushort quantity)
        {
            Quantity = (ushort)Math.Min(MaxQuantity, Quantity + quantity);
        }
    }
    internal class InventorySlotNew : InventorySlot
    {
        public bool New;

        public InventorySlotNew(PBEItem item, ushort quantity)
            : base(item, quantity)
        {
            New = true;
        }
    }

    [DebuggerDisplay("PouchType={" + nameof(PouchType) + "}, Count={" + nameof(Count) + "}")]
    internal sealed class InventoryPouch<T> : IReadOnlyList<T>, IEnumerable<T>, IEnumerable where T : InventorySlot
    {
        public readonly ItemPouchType PouchType;
        private readonly List<T> _items;

        public T this[int index] => _items[index];
        public T this[PBEItem item]
        {
            get
            {
                foreach (T slot in _items)
                {
                    if (slot.Item == item)
                    {
                        return slot;
                    }
                }
                return null;
            }
        }
        public int Count => _items.Count;

        public InventoryPouch(ItemPouchType type)
        {
            PouchType = type;
            _items = new List<T>();
        }

        public void Add(T slot)
        {
            _items.Add(slot);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }

    [DebuggerDisplay("NumPouches={" + nameof(Count) + "}")]
    internal abstract class Inventory<T> : IReadOnlyDictionary<ItemPouchType, InventoryPouch<T>> where T : InventorySlot
    {
        private readonly Dictionary<ItemPouchType, InventoryPouch<T>> _pouches;

        public InventoryPouch<T> this[ItemPouchType pt] => _pouches[pt];
        public IEnumerable<ItemPouchType> Keys => _pouches.Keys;
        public IEnumerable<InventoryPouch<T>> Values => _pouches.Values;
        public int Count => _pouches.Count;

        protected Inventory(params InventoryPouch<T>[] pouches)
        {
            _pouches = new Dictionary<ItemPouchType, InventoryPouch<T>>(pouches.Length);
            foreach (InventoryPouch<T> p in pouches)
            {
                _pouches.Add(p.PouchType, p);
            }
        }

        public abstract void Add(PBEItem item, ushort quantity = 1);

        public bool ContainsKey(ItemPouchType key)
        {
            return _pouches.ContainsKey(key);
        }
        public bool TryGetValue(ItemPouchType key, out InventoryPouch<T> value)
        {
            return _pouches.TryGetValue(key, out value);
        }
        public IEnumerator<KeyValuePair<ItemPouchType, InventoryPouch<T>>> GetEnumerator()
        {
            return _pouches.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _pouches.GetEnumerator();
        }
    }

    internal sealed class Inventory : Inventory<InventorySlot>
    {
        public Inventory()
            : base(new InventoryPouch<InventorySlot>(ItemPouchType.FreeSpace))
        {
        }

        public override void Add(PBEItem item, ushort quantity = 1)
        {
            if (quantity == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least one.");
            }
            InventoryPouch<InventorySlot> pouch = this[0];
            InventorySlot slot = pouch[item];
            if (slot == null)
            {
                pouch.Add(new InventorySlot(item, quantity));
            }
            else
            {
                slot.Add(quantity);
            }
        }
    }
    internal sealed class PlayerInventory : Inventory<InventorySlotNew>
    {
        public PlayerInventory()
            : base(Enum.GetValues(typeof(ItemPouchType)).Cast<ItemPouchType>()
                  .Select(t => new InventoryPouch<InventorySlotNew>(t)).ToArray()) // All pouch types
        {
        }

        public override void Add(PBEItem item, ushort quantity = 1)
        {
            if (quantity == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least one.");
            }
            ItemPouchType pt = ItemData.GetPouchType(item);
            InventoryPouch<InventorySlotNew> pouch = this[pt];
            InventorySlotNew slot = pouch[item];
            if (slot == null)
            {
                pouch.Add(new InventorySlotNew(item, quantity));
            }
            else
            {
                slot.Add(quantity);
            }
        }
    }
}
