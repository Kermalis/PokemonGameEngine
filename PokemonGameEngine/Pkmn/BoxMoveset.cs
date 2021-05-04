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

            public BoxMovesetSlot() { }
            public BoxMovesetSlot(Moveset.MovesetSlot other)
            {
                Move = other.Move;
                PPUps = other.PPUps;
            }

            public void Clear()
            {
                Move = PBEMove.None;
                PPUps = 0;
            }
        }

        private readonly BoxMovesetSlot[] _slots;

        public BoxMovesetSlot this[int index] => _slots[index];
        public int Count => _slots.Length;

        public BoxMoveset()
        {
            _slots = new BoxMovesetSlot[PkmnConstants.NumMoves];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new BoxMovesetSlot();
            }
        }
        public BoxMoveset(Moveset other)
        {
            _slots = new BoxMovesetSlot[PkmnConstants.NumMoves];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new BoxMovesetSlot(other[i]);
            }
        }

        public bool Contains(PBEMove move)
        {
            return IndexOf(move) != -1;
        }
        public int IndexOf(PBEMove move)
        {
            for (int i = 0; i < PkmnConstants.NumMoves; i++)
            {
                if (_slots[i].Move == move)
                {
                    return i;
                }
            }
            return -1;
        }

        ///<summary>Forgets the move on top, and moves all of the others up once. The last slot will be empty</summary>
        public void ShiftMovesUp()
        {
            for (int i = 1; i < PkmnConstants.NumMoves; i++)
            {
                BoxMovesetSlot above = _slots[i - 1];
                BoxMovesetSlot below = _slots[i];
                above.Move = below.Move;
                above.PPUps = below.PPUps;
            }
            BoxMovesetSlot bottom = _slots[PkmnConstants.NumMoves - 1];
            bottom.Clear();
        }

        public int GetFirstEmptySlot()
        {
            return IndexOf(PBEMove.None);
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
