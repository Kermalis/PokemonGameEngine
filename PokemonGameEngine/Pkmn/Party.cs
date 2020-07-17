using Kermalis.PokemonBattleEngine.Data;
using System.Collections;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class Party : IPBEPartyPokemonCollection<PartyPokemon>, IPBEPartyPokemonCollection, IPBEPokemonCollection<PartyPokemon>, IPBEPokemonCollection
    {
        private readonly List<PartyPokemon> _slots = new List<PartyPokemon>();

        public PartyPokemon this[int index] => _slots[index];
        IPBEPartyPokemon IReadOnlyList<IPBEPartyPokemon>.this[int index] => _slots[index];
        IPBEPokemon IReadOnlyList<IPBEPokemon>.this[int index] => _slots[index];
        public int Count => _slots.Count;

        public void Add(PartyPokemon pkmn)
        {
            _slots.Add(pkmn);
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
