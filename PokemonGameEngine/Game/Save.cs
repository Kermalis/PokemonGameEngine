using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;

namespace Kermalis.PokemonGameEngine.Game
{
    internal sealed class Save
    {
        public Flags Flags { get; }
        public string PlayerName { get; }
        public bool PlayerIsFemale { get; }
        public Party PlayerParty { get; }
        public PlayerInventory PlayerInventory { get; }

        public Save()
        {
            Flags = new Flags();
            PlayerName = "Dawn";
            PlayerIsFemale = true;
            PlayerParty = new Party() { PartyPokemon.GetTestPokemon(PBESpecies.Skitty, 0, PBESettings.DefaultMaxLevel) };
            PlayerInventory = new PlayerInventory();
        }

        // TODO: If party is full, send to a box, if boxes are full, error
        public void GivePokemon(PartyPokemon pkmn)
        {
            PlayerParty.Add(pkmn);
        }
    }
}
