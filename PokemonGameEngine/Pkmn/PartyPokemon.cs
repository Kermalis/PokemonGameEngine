using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Legality;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.Pkmn.Wild;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class PartyPokemon : IPBEPartyPokemon
    {
        public ushort HP { get; set; }

        public PBEStatus1 Status1 { get; set; }
        public byte SleepTurns { get; set; }

        public Moveset Moveset { get; set; }
        IPBEMoveset IPBEPokemon.Moveset => Moveset;
        IPBEPartyMoveset IPBEPartyPokemon.Moveset => Moveset;

        public PBESpecies Species { get; set; }
        public PBEForm Form { get; set; }
        public PBEGender Gender { get; set; }

        public string Nickname { get; set; }
        public bool Shiny { get; set; }
        public byte Level { get; set; }
        public byte Friendship { get; set; }

        public PBEItem Item { get; set; }
        public PBEAbility Ability { get; set; }
        public PBENature Nature { get; set; }

        public EVs EffortValues { get; set; }
        IPBEStatCollection IPBEPokemon.EffortValues => EffortValues;
        public IVs IndividualValues { get; set; }
        IPBEReadOnlyStatCollection IPBEPokemon.IndividualValues => IndividualValues;

        public void RandomizeMoves()
        {
            var moves = new List<PBEMove>(PBELegalityChecker.GetLegalMoves(Species, Form, Level, PBESettings.DefaultSettings));
            int i = 0;
            for (; i < Moveset.Count; i++)
            {
                Moveset[i].Clear();
            }
            for (i = 0; i < Moveset.Count && moves.Count > 0; i++)
            {
                Moveset.MovesetSlot slot = Moveset[i];
                PBEMove move = PBEUtils.GlobalRandom.RandomElement(moves);
                moves.Remove(move);
                slot.Move = move;
                slot.PPUps = 0;
                slot.PP = PBEDataUtils.CalcMaxPP(move, slot.PPUps, PBESettings.DefaultSettings);
            }
        }

        private static PartyPokemon GetTest(PBESpecies species, PBEForm form, byte level)
        {
            var pData = PBEPokemonData.GetData(species, form);
            var p = new PartyPokemon
            {
                Status1 = PBEStatus1.Paralyzed,
                Moveset = new Moveset(),
                Species = species,
                Form = form,
                Nickname = PBELocalizedString.GetSpeciesName(species).English,
                Shiny = PBEUtils.GlobalRandom.RandomShiny(),
                Level = level,
                Item = PBEUtils.GlobalRandom.RandomElement(PBEDataUtils.GetValidItems(species, form)),
                Ability = PBEUtils.GlobalRandom.RandomElement(pData.Abilities),
                Nature = PBEUtils.GlobalRandom.RandomElement(PBEDataUtils.AllNatures),
                EffortValues = new EVs(),
                IndividualValues = new IVs()
            };
            p.HP = PBEDataUtils.CalculateStat(pData, PBEStat.HP, p.Nature, p.EffortValues.HP, p.IndividualValues.HP, p.Level, PBESettings.DefaultSettings);
            p.RandomizeMoves();
            return p;
        }
        public static PartyPokemon GetTestPokemon(PBESpecies species, PBEForm form)
        {
            return GetTest(species, form, PBESettings.DefaultMaxLevel);
        }
        public static PartyPokemon GetTestWildPokemon(WildEncounter encounter)
        {
            return GetTest(encounter.Species, encounter.Form, (byte)PBEUtils.GlobalRandom.RandomInt(encounter.MinLevel, encounter.MaxLevel));
        }
    }
}
