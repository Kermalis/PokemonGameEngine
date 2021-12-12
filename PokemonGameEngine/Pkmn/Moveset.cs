﻿using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using System.Collections;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class Moveset : IPBEPartyMoveset, IPBEPartyMoveset<Moveset.MovesetSlot>, IPBEMoveset, IPBEMoveset<Moveset.MovesetSlot>
    {
        public sealed class MovesetSlot : IPBEPartyMovesetSlot
        {
            public PBEMove Move { get; set; }
            public int PP { get; set; }
            public byte PPUps { get; set; }

            public MovesetSlot() { }
            public MovesetSlot(MovesetSlot other)
            {
                Move = other.Move;
                PPUps = other.PPUps;
                PP = other.PP;
            }
            public MovesetSlot(BoxMoveset.BoxMovesetSlot other)
            {
                Move = other.Move;
                PPUps = other.PPUps;
                SetMaxPP();
            }

            public void Clear()
            {
                Move = PBEMove.None;
                PP = 0;
                PPUps = 0;
            }

            public void SetMaxPP()
            {
                PP = PBEDataUtils.CalcMaxPP(Move, PPUps, PkmnConstants.PBESettings);
            }

            public void UpdateFromBattle(PBEBattleMoveset.PBEBattleMovesetSlot slot)
            {
                Move = slot.Move;
                PP = slot.PP;
            }
        }

        private readonly MovesetSlot[] _slots;

        public MovesetSlot this[int index] => _slots[index];
        IPBEPartyMovesetSlot IReadOnlyList<IPBEPartyMovesetSlot>.this[int index] => _slots[index];
        IPBEMovesetSlot IReadOnlyList<IPBEMovesetSlot>.this[int index] => _slots[index];
        public int Count => _slots.Length;

        public Moveset()
        {
            _slots = new MovesetSlot[PkmnConstants.NumMoves];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new MovesetSlot();
            }
        }
        public Moveset(Moveset other)
        {
            _slots = new MovesetSlot[PkmnConstants.NumMoves];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new MovesetSlot(other[i]);
            }
        }
        public Moveset(BoxMoveset other)
        {
            _slots = new MovesetSlot[PkmnConstants.NumMoves];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new MovesetSlot(other[i]);
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

        public int GetFirstEmptySlot()
        {
            for (int i = 0; i < PkmnConstants.NumMoves; i++)
            {
                if (_slots[i].Move == PBEMove.None)
                {
                    return i;
                }
            }
            return -1;
        }

        public void UpdateFromBattle(PBEBattleMoveset moves)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].UpdateFromBattle(moves[i]);
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
