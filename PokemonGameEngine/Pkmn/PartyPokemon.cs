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

        public ushort HP { get; set; }
        public PBEStatus1 Status1 { get; set; }
        public byte SleepTurns { get; set; }

        public Moveset Moveset { get; set; }
        IPBEMoveset IPBEPokemon.Moveset => Moveset;
        IPBEPartyMoveset IPBEPartyPokemon.Moveset => Moveset;

        public EVs EffortValues { get; set; }
        IPBEStatCollection IPBEPokemon.EffortValues => EffortValues;
        public IVs IndividualValues { get; set; }
        IPBEReadOnlyStatCollection IPBEPokemon.IndividualValues => IndividualValues;

        public ushort MaxHP { get; private set; }
        public ushort Attack { get; private set; }
        public ushort Defense { get; private set; }
        public ushort SpAttack { get; private set; }
        public ushort SpDefense { get; private set; }
        public ushort Speed { get; private set; }

        public PartyPokemon(PBESpecies species, PBEForm form, byte level)
        {
            IPBEPokemonData pData = PBEDataProvider.Instance.GetPokemonData(species, form);
            Species = species;
            Form = form;
            Nickname = PBELocalizedString.GetSpeciesName(species).English;
            Shiny = PBEDataProvider.GlobalRandom.RandomShiny();
            Level = level;
            Ability = PBEDataProvider.GlobalRandom.RandomElement(pData.Abilities);
            Gender = PBEDataProvider.GlobalRandom.RandomGender(pData.GenderRatio);
            Nature = PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.AllNatures);
            Moveset = new Moveset();
            EffortValues = new EVs();
            IndividualValues = new IVs();
            UpdateTimeBasedForms(DateTime.Now);
            SetWildMoves();
            CaughtBall = PBEItem.PokeBall;
            Friendship = byte.MaxValue; // TODO: Default friendship
            CalcStats(pData);
            SetMaxHP();
        }
        public PartyPokemon(EncounterTable.Encounter encounter)
        {
            Species = encounter.Species;
            Form = encounter.Form;
            IPBEPokemonData pData = PBEDataProvider.Instance.GetPokemonData(this);
            Gender = PBEDataProvider.GlobalRandom.RandomGender(pData.GenderRatio);
            Nickname = PBELocalizedString.GetSpeciesName(Species).English;
            Shiny = PBEDataProvider.GlobalRandom.RandomShiny();
            Level = (byte)PBEDataProvider.GlobalRandom.RandomInt(encounter.MinLevel, encounter.MaxLevel);
            Nature = PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.AllNatures);
            Moveset = new Moveset();
            EffortValues = new EVs();
            IndividualValues = new IVs();
            UpdateTimeBasedForms(DateTime.Now);
            SetWildMoves();
            CalcStats(pData);
            SetMaxHP();
        }

        public void SetMaxHP()
        {
            HP = MaxHP;
        }
        private void CalcStats(IPBEPokemonData pData)
        {
            MaxHP = PBEDataUtils.CalculateStat(pData, PBEStat.HP, Nature, EffortValues.HP, IndividualValues.HP, Level, PBESettings.DefaultSettings);
            Attack = PBEDataUtils.CalculateStat(pData, PBEStat.Attack, Nature, EffortValues.Attack, IndividualValues.Attack, Level, PBESettings.DefaultSettings);
            Defense = PBEDataUtils.CalculateStat(pData, PBEStat.Defense, Nature, EffortValues.Defense, IndividualValues.Defense, Level, PBESettings.DefaultSettings);
            SpAttack = PBEDataUtils.CalculateStat(pData, PBEStat.SpAttack, Nature, EffortValues.SpAttack, IndividualValues.SpAttack, Level, PBESettings.DefaultSettings);
            SpDefense = PBEDataUtils.CalculateStat(pData, PBEStat.SpDefense, Nature, EffortValues.SpDefense, IndividualValues.SpDefense, Level, PBESettings.DefaultSettings);
            Speed = PBEDataUtils.CalculateStat(pData, PBEStat.Speed, Nature, EffortValues.Speed, IndividualValues.Speed, Level, PBESettings.DefaultSettings);
        }
        public void CalcStats()
        {
            IPBEPokemonData pData = PBEDataProvider.Instance.GetPokemonData(this);
            CalcStats(pData);
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

        // Temp function to get completely random moves
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
                PBEMove move = PBEDataProvider.GlobalRandom.RandomElement(moves);
                moves.Remove(move);
                slot.Move = move;
                slot.PPUps = 0;
                slot.SetMaxPP();
            }
        }
        private void SetWildMoves(IPBEPokemonData pData)
        {
            // Get last 4 moves that can be learned by level up, with no repeats (such as Sketch)
            PBEMove[] moves = pData.LevelUpMoves.Where(t => t.Level <= Level && t.ObtainMethod.HasFlag(PBEMoveObtainMethod.LevelUp_B2W2) && PBEDataUtils.IsMoveUsable(t.Move))
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
        private void SetWildMoves()
        {
            SetWildMoves(PBEDataProvider.Instance.GetPokemonData(this));
        }

        // TODO: Burmy areas. (Giratina would work similarly if you wanted, with an additional || for the orb)
        public void UpdateTimeBasedForms(DateTime time)
        {
            Month month = OverworldTime.GetMonth((Month)time.Month);
            Season season = OverworldTime.GetSeason(month);
            int hour = OverworldTime.GetHour(time.Hour);
            TimeOfDay tod = OverworldTime.GetTimeOfDay(season, hour);
            UpdateSeasonalForm(season);
            UpdateShayminForm(tod);
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

        public void UpdateFromBattle(PBEBattlePokemon pkmn)
        {
            HP = pkmn.HP;
            Status1 = pkmn.Status1;
            SleepTurns = pkmn.SleepTurns;
            Moveset.UpdateFromBattle(pkmn.Moves);
            Form = pkmn.RevertForm;
            Friendship = pkmn.Friendship;
            Item = pkmn.Item;
            Ability = pkmn.RevertAbility;
            EffortValues.CopyFrom(pkmn.EffortValues);
            CalcStats();
        }
        public void UpdateFromBattle_Caught(PBEBattlePokemon pkmn)
        {
            // TODO: Default friendship (not applied if caught in a friend ball)
            CaughtBall = pkmn.CaughtBall;
            switch (CaughtBall)
            {
                case PBEItem.FriendBall:
                {
                    Friendship = 200;
                    break;
                }
                case PBEItem.HealBall:
                {
                    HealFully();
                    break;
                }
            }
        }
    }
}
