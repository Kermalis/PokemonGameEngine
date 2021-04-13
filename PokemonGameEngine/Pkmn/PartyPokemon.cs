using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class PartyPokemon : IPBEPartyPokemon
    {
        public OTInfo OT { get; set; }

        public PBESpecies Species { get; set; }
        public PBEForm Form { get; set; }
        public PBEGender Gender { get; set; }

        public string Nickname { get; set; }
        public bool Shiny { get; set; }
        public byte Level { get; set; }
        public uint EXP { get; set; }
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

        public uint PID { get; private set; } // Currently only used for Spinda spots; has no other effect

        public PartyPokemon(PBESpecies species, PBEForm form, byte level, OTInfo ot)
        {
            RandomPID();
            OT = ot;
            var pData = new BaseStats(species, form);
            Species = species;
            Form = form;
            Nickname = PBELocalizedString.GetSpeciesName(species).English;
            Shiny = PBEDataProvider.GlobalRandom.RandomShiny();
            Level = level;
            EXP = PBEDataProvider.Instance.GetEXPRequired(pData.GrowthRate, level);
            Ability = PBEDataProvider.GlobalRandom.RandomElement(pData.Abilities);
            Gender = PBEDataProvider.GlobalRandom.RandomGender(pData.GenderRatio);
            Nature = PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.AllNatures);
            Moveset = new Moveset();
            EffortValues = new EVs();
            IndividualValues = new IVs();
            UpdateTimeBasedForms(DateTime.Now);
            SetWildMoves();
            CaughtBall = PBEItem.PokeBall;
            SetDefaultFriendship(pData);
            CalcStats(pData.Stats);
            SetMaxHP();
        }
        public PartyPokemon(EncounterTable.Encounter encounter)
        {
            RandomPID();
            Species = encounter.Species;
            Form = encounter.Form;
            var pData = new BaseStats(Species, Form);
            Gender = PBEDataProvider.GlobalRandom.RandomGender(pData.GenderRatio);
            Nickname = PBELocalizedString.GetSpeciesName(Species).English;
            Shiny = PBEDataProvider.GlobalRandom.RandomShiny();
            Level = (byte)PBEDataProvider.GlobalRandom.RandomInt(encounter.MinLevel, encounter.MaxLevel);
            EXP = PBEDataProvider.Instance.GetEXPRequired(pData.GrowthRate, Level);
            Nature = PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.AllNatures);
            Moveset = new Moveset();
            EffortValues = new EVs();
            IndividualValues = new IVs();
            UpdateTimeBasedForms(DateTime.Now);
            SetWildMoves();
            CalcStats(pData.Stats);
            SetMaxHP();
        }
        public PartyPokemon(BoxPokemon other)
        {
            PID = other.PID;
            OT = other.OT;
            Species = other.Species;
            Form = other.Form;
            Nickname = other.Nickname;
            Shiny = other.Shiny;
            Level = other.Level;
            EXP = other.EXP;
            Ability = other.Ability;
            Gender = other.Gender;
            Nature = other.Nature;
            Moveset = new Moveset(other.Moveset);
            EffortValues = other.EffortValues;
            IndividualValues = other.IndividualValues;
            CaughtBall = other.CaughtBall;
            Friendship = other.Friendship;
            UpdateTimeBasedForms(DateTime.Now);
            CalcStats();
            SetMaxHP();
        }

        private void RandomPID()
        {
            PID = (uint)PBEDataProvider.GlobalRandom.RandomInt();
        }
        private void SetDefaultFriendship(BaseStats pData)
        {
            Friendship = pData.BaseFriendship;
        }

        public void SetMaxHP()
        {
            HP = MaxHP;
        }
        private void CalcStats(IPBEReadOnlyStatCollection baseStats)
        {
            MaxHP = PBEDataUtils.CalculateStat(Species, baseStats, PBEStat.HP, Nature, EffortValues.HP, IndividualValues.HP, Level, PkmnConstants.PBESettings);
            Attack = PBEDataUtils.CalculateStat(Species, baseStats, PBEStat.Attack, Nature, EffortValues.Attack, IndividualValues.Attack, Level, PkmnConstants.PBESettings);
            Defense = PBEDataUtils.CalculateStat(Species, baseStats, PBEStat.Defense, Nature, EffortValues.Defense, IndividualValues.Defense, Level, PkmnConstants.PBESettings);
            SpAttack = PBEDataUtils.CalculateStat(Species, baseStats, PBEStat.SpAttack, Nature, EffortValues.SpAttack, IndividualValues.SpAttack, Level, PkmnConstants.PBESettings);
            SpDefense = PBEDataUtils.CalculateStat(Species, baseStats, PBEStat.SpDefense, Nature, EffortValues.SpDefense, IndividualValues.SpDefense, Level, PkmnConstants.PBESettings);
            Speed = PBEDataUtils.CalculateStat(Species, baseStats, PBEStat.Speed, Nature, EffortValues.Speed, IndividualValues.Speed, Level, PkmnConstants.PBESettings);
        }
        public void CalcStats()
        {
            CalcStats(new BaseStats(Species, Form).Stats);
        }
        public void HealStatus()
        {
            Status1 = PBEStatus1.None;
            SleepTurns = 0;
        }
        public void HealMoves()
        {
            for (int i = 0; i < PkmnConstants.NumMoves; i++)
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
        public void Debug_RandomizeMoves()
        {
            var moves = new List<PBEMove>(new LevelUpData(Species, Form).Moves.Where(t => t.Level <= Level && PBEDataUtils.IsMoveUsable(t.Move)).Select(t => t.Move).Distinct());
            for (int i = 0; i < PkmnConstants.NumMoves; i++)
            {
                Moveset[i].Clear();
            }
            for (int i = 0; i < PkmnConstants.NumMoves && moves.Count > 0; i++)
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
            PBEMove[] moves = new LevelUpData(Species, Form).GetWildMoves(Level);
            for (int i = 0; i < PkmnConstants.NumMoves; i++)
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

        private void UpdateAbilityAndCalcStatsAfterFormChange()
        {
            var bs = new BaseStats(Species, Form);
            CalcStats(bs.Stats);
            if (!bs.Abilities.Contains(Ability))
            {
                Ability = bs.Abilities[0];
            }
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
                Form = season.ToDeerlingSawsbuckForm();
                UpdateAbilityAndCalcStatsAfterFormChange();
            }
        }
        public void UpdateShayminForm(TimeOfDay tod)
        {
            if (tod == TimeOfDay.Night && Species == PBESpecies.Shaymin && Form == PBEForm.Shaymin_Sky)
            {
                Form = PBEForm.Shaymin;
                UpdateAbilityAndCalcStatsAfterFormChange();
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
            Level = pkmn.Level;
            EXP = pkmn.EXP;
            CalcStats();
        }
        public void UpdateFromBattle_Caught(PBEBattlePokemon pkmn)
        {
            OT = Game.Instance.Save.OT;

            CaughtBall = pkmn.CaughtBall;
            if (CaughtBall == PBEItem.FriendBall)
            {
                Friendship = 200;
            }
            else
            {
                var bs = new BaseStats(Species, Form);
                SetDefaultFriendship(bs);
            }

            if (CaughtBall == PBEItem.HealBall)
            {
                HealFully();
            }
        }
    }
}
