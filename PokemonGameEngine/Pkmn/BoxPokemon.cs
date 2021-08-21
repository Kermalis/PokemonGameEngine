using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.World;
using System;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class BoxPokemon : IPBESpeciesForm
    {
        public bool IsEgg { get; set; }
        public uint PID { get; set; }
        public OTInfo OT { get; set; }
        public MapSection MetLocation { get; set; }
        public byte MetLevel { get; set; }
        public DateTime MetDate { get; set; }

        public PBESpecies Species { get; set; }
        public PBEForm Form { get; set; }
        public PBEGender Gender { get; set; }

        public string Nickname { get; set; }
        public bool Shiny { get; set; }
        public byte Level { get; set; }
        public uint EXP { get; set; }
        public byte Friendship { get; set; }
        public ItemType CaughtBall { get; set; }

        public Pokerus Pokerus { get; set; }
        public ItemType Item { get; set; }
        public AbilityType AbilType { get; private set; }
        public PBEAbility Ability { get; set; }
        public PBENature Nature { get; set; }

        public BoxMoveset Moveset { get; set; }
        public EVs EffortValues { get; set; }
        public IVs IndividualValues { get; set; }

        private BoxPokemon() { }
        public BoxPokemon(PartyPokemon other)
        {
            IsEgg = other.IsEgg;
            PID = other.PID;
            Pokerus = new Pokerus(other.Pokerus);
            OT = other.OT;
            MetLocation = other.MetLocation;
            MetLevel = other.MetLevel;
            MetDate = other.MetDate;
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
            AbilType = other.AbilType;
            Ability = other.Ability;
            Nature = other.Nature;
            Moveset = new BoxMoveset(other.Moveset);
            EffortValues = other.EffortValues;
            IndividualValues = other.IndividualValues;
        }

        public static BoxPokemon CreateDaycareEgg(PBESpecies species, PBEForm form, PBEGender gender, byte cycles, byte level, uint exp, bool shiny,
            PBENature nature, (AbilityType Type, PBEAbility Abil) ability, IVs ivs, BoxMoveset moves)
        {
            var p = new BoxPokemon();
            p.PID = (uint)PBEDataProvider.GlobalRandom.RandomInt();
            p.Pokerus = new Pokerus(true);
            p.IsEgg = true;
            p.Level = level;
            p.Nickname = "Egg";
            p.Species = species;
            p.Form = form;
            p.EffortValues = new EVs();
            p.CaughtBall = ItemType.PokeBall;
            p.OT = Engine.Instance.Save.OT;
            p.MetLocation = MapSection.TestMapC; // Egg met location
            p.MetLevel = level;
            p.MetDate = Game.LogicTickTime.Date;
            p.Gender = gender;
            p.Friendship = cycles;
            p.EXP = exp;
            p.Shiny = shiny;
            p.Nature = nature;
            p.AbilType = ability.Type;
            p.Ability = ability.Abil;
            p.IndividualValues = ivs;
            p.Moveset = moves;
            return p;
        }
    }
}
