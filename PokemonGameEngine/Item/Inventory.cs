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

        public void Add(ItemType item, ushort quantity)
        {
            if (!TryAdd(item, quantity))
            {
                throw new Exception();
            }
        }
        public bool HasItem(ItemType item, ushort quantity)
        {
            InventorySlot.ValidateQuantity(quantity);
            T slot = this[item];
            if (slot is null)
            {
                return false;
            }
            return slot.HasQuantity(quantity);
        }
        public bool HasRoom(ItemType item, ushort quantity)
        {
            InventorySlot.ValidateQuantity(quantity);
            T slot = this[item];
            if (slot is null)
            {
                return true;
            }
            return slot.HasRoom(quantity);
        }
        public void Remove(ItemType item, ushort quantity)
        {
            if (!TryRemove(item, quantity))
            {
                throw new Exception();
            }
        }
        public bool TryAdd(ItemType item, ushort quantity)
        {
            InventorySlot.ValidateQuantity(quantity);
            T slot = this[item];
            if (slot is null)
            {
                // Create new slot
                object[] args = new object[2] { item, quantity };
                slot = (T)Activator.CreateInstance(typeof(T), args);
                _items.Add(slot);
                return true;
            }
            if (!slot.HasRoom(quantity))
            {
                return false;
            }
            slot.Quantity += quantity;
            return true;
        }
        public bool TryRemove(ItemType item, ushort quantity)
        {
            InventorySlot.ValidateQuantity(quantity);
            T slot = this[item];
            if (slot is null || !slot.HasQuantity(quantity))
            {
                return false;
            }
            if (slot.Quantity == quantity)
            {
                _items.Remove(slot);
            }
            else
            {
                slot.Quantity -= quantity;
            }
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public void ToPBEInventory(List<(PBEItem, uint)> list)
        {
            foreach (T slot in _items)
            {
                list.Add(((PBEItem)slot.Item, slot.Quantity));
            }
        }
        public void FromPBEInventory(PBEBattleInventory inv)
        {
            foreach (T slot in _items)
            {
                ushort qu = (ushort)inv[(PBEItem)slot.Item].Quantity;
                if (qu != 0)
                {
                    slot.Quantity = qu;
                }
                else
                {
                    _items.Remove(slot);
                }
            }
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
            if (!TryAdd(item, quantity))
            {
                throw new Exception();
            }
        }
        public bool HasItem(ItemType item, ushort quantity)
        {
            InventorySlot.ValidateQuantity(quantity);
            if (!TryGetPouch(item, out InventoryPouch<T> pouch))
            {
                return false;
            }
            return pouch.HasItem(item, quantity);
        }
        public bool HasRoom(ItemType item, ushort quantity)
        {
            InventorySlot.ValidateQuantity(quantity);
            if (!TryGetPouch(item, out InventoryPouch<T> pouch))
            {
                return false;
            }
            return pouch.HasRoom(item, quantity);
        }
        public void Remove(ItemType item, ushort quantity)
        {
            if (!TryRemove(item, quantity))
            {
                throw new Exception();
            }
        }
        public bool TryAdd(ItemType item, ushort quantity)
        {
            InventorySlot.ValidateQuantity(quantity);
            if (!TryGetPouch(item, out InventoryPouch<T> pouch))
            {
                return false;
            }
            return pouch.TryAdd(item, quantity);
        }
        public bool TryRemove(ItemType item, ushort quantity)
        {
            InventorySlot.ValidateQuantity(quantity);
            if (!TryGetPouch(item, out InventoryPouch<T> pouch))
            {
                return false;
            }
            return pouch.TryRemove(item, quantity);
        }

        public bool TryGetPouch(ItemType item, out InventoryPouch<T> pouch)
        {
            ItemPouchType pt = ItemData.GetPouchType(item);
            return _pouches.TryGetValue(pt, out pouch);
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
                if (pouch.PouchType != ItemPouchType.FreeSpace
                    && pouch.PouchType != ItemPouchType.KeyItems
                    && pouch.PouchType != ItemPouchType.Mail
                    && pouch.PouchType != ItemPouchType.TMHMs)
                {
                    pouch.ToPBEInventory(list);
                }
            }
            return list;
        }
        public void FromPBEInventory(PBEBattleInventory inv)
        {
            foreach (InventoryPouch<T> pouch in _pouches.Values)
            {
                if (pouch.PouchType != ItemPouchType.FreeSpace
                    && pouch.PouchType != ItemPouchType.KeyItems
                    && pouch.PouchType != ItemPouchType.Mail
                    && pouch.PouchType != ItemPouchType.TMHMs)
                {
                    pouch.FromPBEInventory(inv);
                }
            }
        }
    }
}
