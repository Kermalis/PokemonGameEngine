using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Kermalis.PokemonGameEngine.Item
{
    [DebuggerDisplay("NumPouches={" + nameof(Count) + "}")]
    internal class Inventory<T> : IReadOnlyDictionary<ItemPouchType, InventoryPouch<T>> where T : InventorySlot
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
        public static Inventory<T> CreateGenericInventory()
        {
            // One non-specific pouch
            var pouch = new InventoryPouch<T>(ItemPouchType.FreeSpace);
            return new Inventory<T>(pouch);
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
