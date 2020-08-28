using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Legality;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public PBEItem CaughtBall { get; set; }

        public PBEItem Item { get; set; }
        public PBEAbility Ability { get; set; }
        public PBENature Nature { get; set; }

        public EVs EffortValues { get; set; }
        IPBEStatCollection IPBEPokemon.EffortValues => EffortValues;
        public IVs IndividualValues { get; set; }
        IPBEReadOnlyStatCollection IPBEPokemon.IndividualValues => IndividualValues;

        private void SetMaxHP(IPBEPokemonData pData)
        {
            HP = PBEDataUtils.CalculateStat(pData, PBEStat.HP, Nature, EffortValues.HP, IndividualValues.HP, Level, PBESettings.DefaultSettings);
        }
        public void SetMaxHP()
        {
            IPBEPokemonData pData = PBEDataProvider.Instance.GetPokemonData(this);
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

        private void RandomizeMoves()
        {
            var moves = new List<PBEMove>(PBELegalityChecker.GetLegalMoves(Species, Form, Level, PBESettings.DefaultSettings));
            for (int i = 0; i < PBESettings.DefaultNumMoves; i++)
            {
                Moveset[i].Clear();
            }
            for (int i = 0; i < PBESettings.DefaultNumMoves && moves.Count > 0; i++)
            {
                Moveset.MovesetSlot slot = Moveset[i];
                PBEMove move = PBEDataProvider.GlobalRandom.RandomElement(moves);
                moves.Remove(move);
                slot.Move = move;
                slot.PPUps = 0;
                slot.SetMaxPP();
            }
        }
        private void SetWildMoves()
        {
            // Get last 4 moves that can be learned by level up, with no repeats (such as Sketch)
            PBEMove[] moves = PBEDataProvider.Instance.GetPokemonData(this).LevelUpMoves.Where(t => t.Level <= Level && PBEDataUtils.IsMoveUsable(t.Move))
                .Select(t => t.Move).Distinct().Reverse().Take(PBESettings.DefaultNumMoves).ToArray();
            for (int i = 0; i < PBESettings.DefaultNumMoves; i++)
            {
                Moveset[i].Clear();
            }
            for (int i = 0; i < moves.Length; i++)
            {
                Moveset.MovesetSlot slot = Moveset[i];
                slot.Move = moves[i];
                slot.PPUps = 0;
                slot.SetMaxPP();
            }
        }

        public void UpdateSeasonalForm(Season season)
        {
            if (Species == PBESpecies.Deerling || Species == PBESpecies.Sawsbuck)
            {
                Form = season.ToDeerlingSawsbuckForm(); // TODO: Update stats/ability
            }
        }
        public void UpdateShayminForm(TimeOfDay tod)
        {
            if (tod == TimeOfDay.Night && Species == PBESpecies.Shaymin && Form == PBEForm.Shaymin_Sky)
            {
                Form = PBEForm.Shaymin; // TODO: Update stats/ability
            }
        }

        private static PartyPokemon GetTest(PBESpecies species, PBEForm form, byte level, bool wild)
        {
            IPBEPokemonData pData = PBEDataProvider.Instance.GetPokemonData(species, form);
            var p = new PartyPokemon
            {
                Status1 = PBEStatus1.None,
                Moveset = new Moveset(),
                Species = species,
                Form = form,
                Nickname = PBELocalizedString.GetSpeciesName(species).English,
                Shiny = PBEDataProvider.GlobalRandom.RandomShiny(),
                Level = level,
                Item = PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.GetValidItems(species, form)),
                Ability = PBEDataProvider.GlobalRandom.RandomElement(pData.Abilities),
                Gender = PBEDataProvider.GlobalRandom.RandomGender(pData.GenderRatio),
                Nature = PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.AllNatures),
                EffortValues = new EVs(),
                IndividualValues = new IVs()
            };
            // TODO: Burmy/Wormadam areas. (Giratina would work similarly if you wanted, with an additional || for the orb)
            DateTime time = DateTime.Now;
            Month month = OverworldTime.GetMonth((Month)time.Month);
            Season season = OverworldTime.GetSeason(month);
            int hour = OverworldTime.GetHour(time.Hour);
            TimeOfDay tod = OverworldTime.GetTimeOfDay(season, hour);
            p.UpdateSeasonalForm(season);
            p.UpdateShayminForm(tod);
            p.SetMaxHP(pData);
            if (wild)
            {
                p.SetWildMoves();
            }
            else
            {
                p.RandomizeMoves();
                p.CaughtBall = PBEItem.LoveBall;
                p.Friendship = byte.MaxValue;
            }
            return p;
        }
        public static PartyPokemon GetTestPokemon(PBESpecies species, PBEForm form, byte level)
        {
            return GetTest(species, form, level, false);
        }
        public static PartyPokemon GetTestWildPokemon(EncounterTable.Encounter encounter)
        {
            return GetTest(encounter.Species, encounter.Form, (byte)PBEDataProvider.GlobalRandom.RandomInt(encounter.MinLevel, encounter.MaxLevel), true);
        }
    }
}
