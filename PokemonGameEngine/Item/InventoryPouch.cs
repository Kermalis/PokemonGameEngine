using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Kermalis.PokemonGameEngine.Item
{
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
}
