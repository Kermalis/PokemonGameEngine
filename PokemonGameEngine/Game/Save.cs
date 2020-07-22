using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Pkmn;

namespace Kermalis.PokemonGameEngine.Game
{
    internal class Save
    {
        public static Save Instance { get; } = new Save();

        public string PlayerName { get; }
        public Party PlayerParty { get; }

        private Save()
        {
            PlayerName = "Dawn";
            PlayerParty = new Party() { PartyPokemon.GetTestPokemon(PBESpecies.Skitty, 0) };
        }
    }
}
