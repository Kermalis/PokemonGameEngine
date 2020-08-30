using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class Save
    {
        public Flags Flags { get; }
        public string PlayerName { get; }
        public bool PlayerIsFemale { get; }
        public Party PlayerParty { get; }
        public PlayerInventory PlayerInventory { get; }
        public uint Money { get; private set; }

        public Save()
        {
            Flags = new Flags();
            PlayerName = "Dawn";
            PlayerIsFemale = true;
            PlayerParty = new Party() { new PartyPokemon(PBESpecies.Skitty, 0, PBESettings.DefaultMaxLevel) };
            PlayerParty[0].Moveset[0].Move = PBEMove.Dig;
            PlayerInventory = new PlayerInventory();
            PlayerInventory.Add(PBEItem.PokeBall, 995);
            PlayerInventory.Add(PBEItem.RockyHelmet, 42);
            PlayerInventory.Add(PBEItem.Leftovers, 473);
            PlayerInventory.Add(PBEItem.Potion, 123);
            PlayerInventory.Add(PBEItem.RedScarf, 230);
            PlayerInventory.Add(PBEItem.PokeDoll, 130);
            PlayerInventory.Add(PBEItem.XSpDef, 120);
            PlayerInventory.Add(PBEItem.AirBalloon, 407);
            PlayerInventory.Add(PBEItem.AdamantOrb, 73);
            PlayerInventory.Add(PBEItem.DarkGem, 69);
            PlayerInventory.Add(PBEItem.FluffyTail, 888);
            Money = 473_123;
        }

        // TODO: If party is full, send to a box, if boxes are full, error
        public void GivePokemon(PartyPokemon pkmn)
        {
            PlayerParty.Add(pkmn);
        }
    }
}
