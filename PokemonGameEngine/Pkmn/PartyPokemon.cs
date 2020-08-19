using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Legality;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.World;
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

        private void SetMaxHP(PBEPokemonData pData)
        {
            HP = PBEDataUtils.CalculateStat(pData, PBEStat.HP, Nature, EffortValues.HP, IndividualValues.HP, Level, PBESettings.DefaultSettings);
        }
        public void SetMaxHP()
        {
            var pData = PBEPokemonData.GetData(Species, Form);
            SetMaxHP(pData);
        }
        public void HealStatus()
        {
            Status1 = PBEStatus1.None;
            SleepTurns = 0;
        }
        public void HealMoves()
        {
            for (int i = 0; i < PBESettings.DefaultNumMoves; i++)
            {
                Moveset[i].SetMaxPP();
            }
        }
        public void HealFully()
        {
            SetMaxHP();
            HealStatus();
            HealMoves();
        }

        public void RandomizeMoves()
        {
            var moves = new List<PBEMove>(PBELegalityChecker.GetLegalMoves(Species, Form, Level, PBESettings.DefaultSettings));
            for (int i = 0; i < PBESettings.DefaultNumMoves; i++)
            {
                Moveset[i].Clear();
            }
            for (int i = 0; i < PBESettings.DefaultNumMoves && moves.Count > 0; i++)
            {
                Moveset.MovesetSlot slot = Moveset[i];
                PBEMove move = PBEUtils.GlobalRandom.RandomElement(moves);
                moves.Remove(move);
                slot.Move = move;
                slot.PPUps = 0;
                slot.SetMaxPP();
            }
        }

        private static PartyPokemon GetTest(PBESpecies species, PBEForm form, byte level)
        {
            var pData = PBEPokemonData.GetData(species, form);
            var p = new PartyPokemon
            {
                Status1 = PBEStatus1.None,
                Moveset = new Moveset(),
                Species = species,
                Form = form,
                Nickname = PBELocalizedString.GetSpeciesName(species).English,
                Shiny = PBEUtils.GlobalRandom.RandomShiny(),
                Level = level,
                Item = PBEUtils.GlobalRandom.RandomElement(PBEDataUtils.GetValidItems(species, form)),
                Ability = PBEUtils.GlobalRandom.RandomElement(pData.Abilities),
                Gender = PBEUtils.GlobalRandom.RandomGender(pData.GenderRatio),
                Nature = PBEUtils.GlobalRandom.RandomElement(PBEDataUtils.AllNatures),
                EffortValues = new EVs(),
                IndividualValues = new IVs()
            };
            p.SetMaxHP(pData);
            p.RandomizeMoves();
            return p;
        }
        public static PartyPokemon GetTestPokemon(PBESpecies species, PBEForm form, byte level)
        {
            return GetTest(species, form, level);
        }
        public static PartyPokemon GetTestWildPokemon(EncounterTable.Encounter encounter)
        {
            return GetTest(encounter.Species, encounter.Form, (byte)PBEUtils.GlobalRandom.RandomInt(encounter.MinLevel, encounter.MaxLevel));
        }
    }
}
