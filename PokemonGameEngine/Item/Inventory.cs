using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

        public virtual void Add(ushort quantity)
        {
            Quantity = (ushort)Math.Min(MaxQuantity, Quantity + quantity);
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

    [DebuggerDisplay("PouchType={" + nameof(PouchType) + "}, Count={" + nameof(Count) + "}")]
    internal sealed class InventoryPouch<T> : IReadOnlyList<T>, IEnumerable<T>, IEnumerable where T : InventorySlot
    {
        public readonly ItemPouchType PouchType;
        private readonly List<T> _items;

        public T this[int index] => _items[index];
        public T this[ItemType item]
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
        public void Remove(T slot)
        {
            _items.Remove(slot);
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
    internal sealed class Inventory<T> : IReadOnlyDictionary<ItemPouchType, InventoryPouch<T>> where T : InventorySlot
    {
        private readonly Dictionary<ItemPouchType, InventoryPouch<T>> _pouches;

        public InventoryPouch<T> this[ItemPouchType pt] => _pouches[pt];
        public IEnumerable<ItemPouchType> Keys => _pouches.Keys;
        public IEnumerable<InventoryPouch<T>> Values => _pouches.Values;
        public int Count => _pouches.Count;

        private Inventory(params InventoryPouch<T>[] pouches)
        {
            _pouches = new Dictionary<ItemPouchType, InventoryPouch<T>>(pouches.Length);
            foreach (InventoryPouch<T> p in pouches)
            {
                _pouches.Add(p.PouchType, p);
            }
        }
        public static Inventory<T> CreateGenericInventory()
        {
            // One non-specific pouch
            var pouch = new InventoryPouch<T>(ItemPouchType.FreeSpace);
            return new Inventory<T>(pouch);
        }
        public static Inventory<T> CreatePlayerInventory()
        {
            // All pouch types
            InventoryPouch<T>[] pouches = Enum.GetValues<ItemPouchType>().Select(t => new InventoryPouch<T>(t)).ToArray();
            return new Inventory<T>(pouches);
        }

        public void Add(ItemType item, ushort quantity)
        {
            if (quantity == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least one.");
            }
            ItemPouchType pt = ItemData.GetPouchType(item);
            if (!_pouches.ContainsKey(pt))
            {
                throw new ArgumentOutOfRangeException(nameof(item), "Item cannot be put in this inventory");
            }
            InventoryPouch<T> pouch = _pouches[pt];
            T slot = pouch[item];
            if (slot == null)
            {
                // Create new slot
                object[] args = new object[2] { item, quantity };
                slot = (T)Activator.CreateInstance(typeof(T), args);
                pouch.Add(slot);
            }
            else
            {
                slot.Add(quantity);
            }
        }
        public bool HasItem(ItemType item, ushort quantity)
        {
            ItemPouchType pt = ItemData.GetPouchType(item);
            InventoryPouch<T> pouch = this[pt];
            T slot = pouch[item];
            if (slot == null)
            {
                return false;
            }
            return slot.Quantity >= quantity;
        }
        public void Remove(ItemType item, ushort quantity)
        {
            ItemPouchType pt = ItemData.GetPouchType(item);
            InventoryPouch<T> pouch = this[pt];
            T slot = pouch[item];
            if (slot == null || slot.Quantity < quantity)
            {
                throw new Exception();
            }
            if (slot.Quantity == quantity)
            {
                pouch.Remove(slot);
            }
            else
            {
                slot.Quantity -= quantity;
            }
        }
        public bool TryRemove(ItemType item, ushort quantity)
        {
            ItemPouchType pt = ItemData.GetPouchType(item);
            InventoryPouch<T> pouch = this[pt];
            T slot = pouch[item];
            if (slot == null || slot.Quantity < quantity)
            {
                return false;
            }
            if (slot.Quantity == quantity)
            {
                pouch.Remove(slot);
            }
            else
            {
                slot.Quantity -= quantity;
            }
            return true;
        }

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

        public List<(PBEItem, uint)> ToPBEInventory()
        {
            var list = new List<(PBEItem, uint)>();
            foreach (InventoryPouch<T> pouch in _pouches.Values)
            {
                if (pouch.PouchType == ItemPouchType.FreeSpace
                    || pouch.PouchType == ItemPouchType.KeyItems
                    || pouch.PouchType == ItemPouchType.Mail
                    || pouch.PouchType == ItemPouchType.TMHMs)
                {
                    continue;
                }
                foreach (T slot in pouch)
                {
                    list.Add(((PBEItem)slot.Item, slot.Quantity));
                }
            }
            return list;
        }
        public void FromPBEInventory(PBEBattleInventory inv)
        {
            foreach (InventoryPouch<T> pouch in _pouches.Values)
            {
                if (pouch.PouchType == ItemPouchType.FreeSpace
                    || pouch.PouchType == ItemPouchType.KeyItems
                    || pouch.PouchType == ItemPouchType.Mail
                    || pouch.PouchType == ItemPouchType.TMHMs)
                {
                    continue;
                }
                foreach (T slot in pouch)
                {
                    ushort qu = (ushort)inv[(PBEItem)slot.Item].Quantity;
                    if (qu != 0)
                    {
                        slot.Quantity = qu;
                    }
                    else
                    {
                        pouch.Remove(slot);
                    }
                }
            }
        }
    }
}
