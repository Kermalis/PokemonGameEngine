using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.World;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class BoxPokemon : IPBESpeciesForm
    {
        public bool IsEgg { get; set; }
        public uint PID { get; set; }
        public OTInfo OT { get; set; }
        public MapSection MetLocation { get; set; }

        public PBESpecies Species { get; set; }
        public PBEForm Form { get; set; }
        public PBEGender Gender { get; set; }

        public string Nickname { get; set; }
        public bool Shiny { get; set; }
        public byte Level { get; set; }
        public uint EXP { get; set; }
        public byte Friendship { get; set; }
        public ItemType CaughtBall { get; set; }

        public ItemType Item { get; set; }
        public PBEAbility Ability { get; set; }
        public PBENature Nature { get; set; }

        public BoxMoveset Moveset { get; set; }
        public EVs EffortValues { get; set; }
        public IVs IndividualValues { get; set; }

        public BoxPokemon() { }
        public BoxPokemon(PartyPokemon other)
        {
            IsEgg = other.IsEgg;
            PID = other.PID;
            OT = other.OT;
            MetLocation = other.MetLocation;
            Species = other.Species;
            Form = other.Form;
            Gender = other.Gender;
            Nickname = other.Nickname;
            Shiny = other.Shiny;
            Level = other.Level;
            EXP = other.EXP;
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
