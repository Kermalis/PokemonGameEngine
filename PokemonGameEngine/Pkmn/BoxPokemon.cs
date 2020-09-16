using Kermalis.PokemonBattleEngine.Data;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class BoxPokemon : IPBESpeciesForm
    {
        public uint PID { get; }

        public PBESpecies Species { get; set; }
        public PBEForm Form { get; set; }
        public PBEGender Gender { get; set; }

        public string Nickname { get; set; }
        public bool Shiny { get; set; }
        public byte Level { get; set; }
        public byte Friendship { get; set; }
        public PBEItem CaughtBall { get; set; }

        public PBEItem Item { get; set; }
        public PBEAbility Ability { get; set; }
        public PBENature Nature { get; set; }

        public BoxMoveset Moveset { get; set; }
        public EVs EffortValues { get; set; }
        public IVs IndividualValues { get; set; }

        public BoxPokemon(PartyPokemon other)
        {
            PID = other.PID;
            Species = other.Species;
            Form = other.Form;
            Gender = other.Gender;
            Nickname = other.Nickname;
            Shiny = other.Shiny;
            Level = other.Level;
            Friendship = other.Friendship;
            CaughtBall = other.CaughtBall;
            Item = other.Item;
            Ability = other.Ability;
            Nature = other.Nature;
            Moveset = new BoxMoveset(other.Moveset);
            EffortValues = other.EffortValues;
            IndividualValues = other.IndividualValues;
        }
    }
}
