﻿using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class Party : IPBEPartyPokemonCollection<PartyPokemon>, IPBEPartyPokemonCollection, IPBEPokemonCollection<PartyPokemon>, IPBEPokemonCollection
    {
        private readonly List<PartyPokemon> _slots = new(PkmnConstants.PartyCapacity);

        public PartyPokemon this[int index] => _slots[index];
        IPBEPartyPokemon IReadOnlyList<IPBEPartyPokemon>.this[int index] => _slots[index];
        IPBEPokemon IReadOnlyList<IPBEPokemon>.this[int index] => _slots[index];
        public int Count => _slots.Count;

        public int Add(PartyPokemon pkmn)
        {
            if (_slots.Count < PkmnConstants.PartyCapacity)
            {
                _slots.Add(pkmn);
                return _slots.Count - 1;
            }
            return -1;
        }

        public int IndexOf(PartyPokemon pkmn)
        {
            return _slots.IndexOf(pkmn);
        }

        public void Remove(PartyPokemon pkmn)
        {
            if (!_slots.Remove(pkmn))
            {
                throw new Exception();
            }
        }

        public void HealFully()
        {
            foreach (PartyPokemon pkmn in _slots)
            {
                pkmn.HealFully();
            }
        }

        public IEnumerator<PartyPokemon> GetEnumerator()
        {
            return _slots.GetEnumerator();
        }
        IEnumerator<IPBEPartyPokemon> IEnumerable<IPBEPartyPokemon>.GetEnumerator()
        {
            return _slots.GetEnumerator();
        }
        IEnumerator<IPBEPokemon> IEnumerable<IPBEPokemon>.GetEnumerator()
        {
            return _slots.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _slots.GetEnumerator();
        }
    }
}
