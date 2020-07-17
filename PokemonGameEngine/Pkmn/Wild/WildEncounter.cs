using Kermalis.PokemonBattleEngine.Data;

namespace Kermalis.PokemonGameEngine.Pkmn.Wild
{
    internal sealed class WildEncounter
    {
        public byte MinLevel { get; set; }
        public byte MaxLevel { get; set; }
        public PBESpecies Species { get; set; }
        public PBEForm Form { get; set; }

        public static WildEncounter GetTestEncounter()
        {
            return new WildEncounter
            {
                MinLevel = 20,
                MaxLevel = 100,
                Species = PBESpecies.Arceus,
                Form = PBEForm.Arceus_Dragon
            };
        }
    }
}
