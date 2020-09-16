using Kermalis.PokemonBattleEngine.Data;
using System.Collections;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class BoxMoveset : IReadOnlyList<BoxMoveset.BoxMovesetSlot>
    {
        public sealed class BoxMovesetSlot
        {
            public PBEMove Move { get; set; }
            public byte PPUps { get; set; }

            public BoxMovesetSlot(Moveset.MovesetSlot other)
            {
                Move = other.Move;
                PPUps = other.PPUps;
            }
        }

        private readonly BoxMovesetSlot[] _slots;

        public BoxMovesetSlot this[int index] => _slots[index];
        public int Count => _slots.Length;

        public BoxMoveset(Moveset other)
        {
            _slots = new BoxMovesetSlot[PkmnConstants.NumMoves];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new BoxMovesetSlot(other[i]);
            }
        }

        IEnumerator<BoxMovesetSlot> IEnumerable<BoxMovesetSlot>.GetEnumerator()
        {
            return ((IEnumerable<BoxMovesetSlot>)_slots).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _slots.GetEnumerator();
        }
    }
}
