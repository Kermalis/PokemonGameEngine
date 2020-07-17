using Kermalis.PokemonBattleEngine.Data;
using System.Collections;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class Moveset : IPBEPartyMoveset<Moveset.MovesetSlot>, IPBEPartyMoveset, IPBEMoveset<Moveset.MovesetSlot>, IPBEMoveset
    {
        public sealed class MovesetSlot : IPBEPartyMovesetSlot, IPBEMovesetSlot
        {
            public PBEMove Move { get; set; }
            public int PP { get; set; }
            public byte PPUps { get; set; }

            public void Clear()
            {
                Move = PBEMove.None;
                PP = 0;
                PPUps = 0;
            }
        }

        private readonly MovesetSlot[] _slots;

        public MovesetSlot this[int index] => _slots[index];
        IPBEPartyMovesetSlot IReadOnlyList<IPBEPartyMovesetSlot>.this[int index] => _slots[index];
        IPBEMovesetSlot IReadOnlyList<IPBEMovesetSlot>.this[int index] => _slots[index];
        public int Count => _slots.Length;

        public Moveset()
        {
            _slots = new MovesetSlot[PBESettings.DefaultNumMoves];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new MovesetSlot();
            }
        }

        IEnumerator<MovesetSlot> IEnumerable<MovesetSlot>.GetEnumerator()
        {
            return ((IEnumerable<MovesetSlot>)_slots).GetEnumerator();
        }
        public IEnumerator<IPBEPartyMovesetSlot> GetEnumerator()
        {
            return ((IEnumerable<MovesetSlot>)_slots).GetEnumerator();
        }
        IEnumerator<IPBEMovesetSlot> IEnumerable<IPBEMovesetSlot>.GetEnumerator()
        {
            return ((IEnumerable<MovesetSlot>)_slots).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _slots.GetEnumerator();
        }
    }
}
