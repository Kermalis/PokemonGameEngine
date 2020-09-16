using System.Collections;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class PCBox : IReadOnlyList<BoxPokemon>
    {
        private readonly BoxPokemon[] _pkmn;
        public string Name;

        public BoxPokemon this[int index] => _pkmn[index];
        public int Count { get; private set; }

        public PCBox(string name)
        {
            Name = name;
            _pkmn = new BoxPokemon[PkmnConstants.BoxCapacity];
            Count = 0;
        }

        public bool Add(PartyPokemon pkmn)
        {
            if (Count < PkmnConstants.BoxCapacity)
            {
                _pkmn[IndexOfEmpty()] = new BoxPokemon(pkmn);
                Count++;
                return true;
            }
            return false;
        }

        public int IndexOf(BoxPokemon pkmn)
        {
            for (int i = 0; i < PkmnConstants.BoxCapacity; i++)
            {
                BoxPokemon p = _pkmn[i];
                if (p == pkmn)
                {
                    return i;
                }
            }
            return -1;
        }
        public int IndexOfEmpty()
        {
            return IndexOf(null);
        }

        public IEnumerator<BoxPokemon> GetEnumerator()
        {
            return ((IEnumerable<BoxPokemon>)_pkmn).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _pkmn.GetEnumerator();
        }
    }
    internal sealed class PCBoxes : IReadOnlyList<PCBox>
    {
        private readonly PCBox[] _boxes;

        public PCBox this[int index] => _boxes[index];
        public int Count => _boxes.Length;

        public PCBoxes()
        {
            _boxes = new PCBox[PkmnConstants.NumBoxes];
            for (int i = 0; i < PkmnConstants.NumBoxes; i++)
            {
                _boxes[i] = new PCBox($"Box {i}");
            }
        }

        public int Add(PartyPokemon pkmn)
        {
            for (int i = 0; i < PkmnConstants.NumBoxes; i++)
            {
                if (_boxes[i].Add(pkmn))
                {
                    return i;
                }
            }
            return -1;
        }

        public IEnumerator<PCBox> GetEnumerator()
        {
            return ((IEnumerable<PCBox>)_boxes).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _boxes.GetEnumerator();
        }
    }
}
