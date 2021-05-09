using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Util;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class PartyPokemon : IPBEPartyPokemon
    {
        public OTInfo OT { get; set; }
        public MapSection MetLocation { get; set; }

        public PBESpecies Species { get; set; }
        public PBEForm Form { get; set; }
        public PBEGender Gender { get; set; }

        public string Nickname { get; set; }
        public bool Shiny { get; set; }
        public byte Level { get; set; }
        public uint EXP { get; set; }
        /// <summary>Remaining egg cycles if <see cref="IsEgg"/> is true.</summary>
        public byte Friendship { get; set; }
        public ItemType CaughtBall { get; set; }

        public ItemType Item { get; set; }
        public PBEAbility Ability { get; set; }
        public PBENature Nature { get; set; }

        public ushort HP { get; set; }
        public PBEStatus1 Status1 { get; set; }
        public byte SleepTurns { get; set; }

        public Moveset Moveset { get; set; }

        public EVs EffortValues { get; set; }
        public IVs IndividualValues { get; set; }

        public ushort MaxHP { get; private set; }
        public ushort Attack { get; private set; }
        public ushort Defense { get; private set; }
        public ushort SpAttack { get; private set; }
        public ushort SpDefense { get; private set; }
        public ushort Speed { get; private set; }

        public uint PID { get; private set; } // Currently only used for Spinda spots and Wurmple evolution
        public bool IsEgg { get; set; }

        #region PBE
        public bool PBEIgnore => IsEgg;
        PBEItem IPBEPokemon.CaughtBall => (PBEItem)CaughtBall;
        PBEItem IPBEPokemon.Item => (PBEItem)Item;
        IPBEStatCollection IPBEPokemon.EffortValues => EffortValues;
        IPBEReadOnlyStatCollection IPBEPokemon.IndividualValues => IndividualValues;
        IPBEMoveset IPBEPokemon.Moveset => Moveset;
        IPBEPartyMoveset IPBEPartyPokemon.Moveset => Moveset;
        #endregion

        #region Creation

        private PartyPokemon(PBESpecies species, PBEForm form, byte level)
        {
            Species = species;
            Form = form;
            Level = level;
        }
        public PartyPokemon(EncounterTable.Encounter encounter)
        {
            SetRandomPID();
            Species = encounter.Species;
            Form = encounter.Form;
            Level = (byte)PBEDataProvider.GlobalRandom.RandomInt(encounter.MinLevel, encounter.MaxLevel);
            SetDefaultNickname();
            Shiny = Utils.GetRandomShiny();
            Nature = PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.AllNatures);
            var pData = new BaseStats(Species, Form);
            SetDefaultEXPForLevel(pData);
            Ability = pData.GetRandomNonHiddenAbility();
            Gender = PBEDataProvider.GlobalRandom.RandomGender(pData.GenderRatio);
            Moveset = new Moveset();
            EffortValues = new EVs();
            IndividualValues = new IVs();
            UpdateTimeBasedForms(DateTime.Now);
            SetDefaultMoves();
            CalcStats(pData.Stats);
            SetMaxHP();
        }
        public PartyPokemon(BoxPokemon other)
        {
            PID = other.PID;
            IsEgg = other.IsEgg;
            OT = other.OT;
            MetLocation = other.MetLocation;
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

        public static PartyPokemon CreatePlayerOwnedMon(PBESpecies species, PBEForm form, byte level)
        {
            var p = new PartyPokemon(species, form, level);
            p.SetRandomPID();
            p.SetPlayerOT();
            p.SetCurrentMetLocation();
            p.SetDefaultNickname();
            p.Shiny = Utils.GetRandomShiny();
            var pData = new BaseStats(species, form);
            p.SetDefaultFriendship(pData);
            p.SetDefaultEXPForLevel(pData);
            p.Ability = pData.GetRandomNonHiddenAbility();
            p.Gender = PBEDataProvider.GlobalRandom.RandomGender(pData.GenderRatio);
            p.Nature = PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.AllNatures);
            p.Moveset = new Moveset();
            p.EffortValues = new EVs();
            p.IndividualValues = new IVs();
            p.UpdateTimeBasedForms(DateTime.Now);
            p.SetDefaultMoves();
            p.CaughtBall = ItemType.PokeBall;
            p.CalcStats(pData.Stats);
            p.SetMaxHP();
            return p;
        }
        public static PartyPokemon CreateWildMon(PBESpecies species, PBEForm form, byte level)
        {
            var p = new PartyPokemon(species, form, level);
            p.SetRandomPID();
            p.SetDefaultNickname();
            p.Shiny = Utils.GetRandomShiny();
            p.Nature = PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.AllNatures);
            var pData = new BaseStats(species, form);
            p.SetDefaultEXPForLevel(pData);
            p.Ability = pData.GetRandomNonHiddenAbility();
            p.Gender = PBEDataProvider.GlobalRandom.RandomGender(pData.GenderRatio);
            p.Moveset = new Moveset();
            p.EffortValues = new EVs();
            p.IndividualValues = new IVs();
            p.UpdateTimeBasedForms(DateTime.Now);
            p.SetDefaultMoves();
            p.SetDefaultFriendship(pData);
            p.CalcStats(pData.Stats);
            p.SetMaxHP();
            return p;
        }
        public static PartyPokemon CreateDefaultEgg(PBESpecies species, PBEForm form)
        {
            var p = new PartyPokemon(species, form, PkmnConstants.EggHatchLevel);
            p.SetRandomPID();
            p.IsEgg = true;
            p.SetPlayerOT();
            p.SetCurrentMetLocation();
            p.Nickname = "Egg";
            p.Shiny = Utils.GetRandomShiny();
            var pData = new BaseStats(species, form);
            p.SetDefaultEggCycles(pData);
            p.SetDefaultEXPForLevel(pData);
            p.Ability = pData.GetRandomNonHiddenAbility();
            p.Gender = PBEDataProvider.GlobalRandom.RandomGender(pData.GenderRatio);
            p.Nature = PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.AllNatures);
            p.Moveset = new Moveset();
            p.EffortValues = new EVs();
            p.IndividualValues = new IVs();
            p.SetDefaultMoves();
            p.CaughtBall = ItemType.PokeBall;
            return p;
        }

        #endregion

        private void SetRandomPID()
        {
            PID = (uint)PBEDataProvider.GlobalRandom.RandomInt();
        }
        private void SetDefaultFriendship(BaseStats pData)
        {
            Friendship = pData.BaseFriendship;
        }
        private void SetDefaultEggCycles(BaseStats pData)
        {
            Friendship = pData.EggCycles;
        }
        private void SetCurrentMetLocation()
        {
            MetLocation = Overworld.GetCurrentLocation();
        }
        private void SetPlayerOT()
        {
            OT = Game.Instance.Save.OT;
        }
        private void SetDefaultNickname()
        {
            Nickname = PBELocalizedString.GetSpeciesName(Species).English;
        }
        private bool HasDefaultNickname()
        {
            return Nickname == PBELocalizedString.GetSpeciesName(Species).English;
        }
        /// <summary>Sets the moves to the last 4 moves the Pokémon would've learned by level-up.</summary>
        private void SetDefaultMoves()
        {
            PBEMove[] moves = new LevelUpData(Species, Form).GetDefaultMoves(Level);
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
        private void SetDefaultEXPForLevel(BaseStats pData)
        {
            EXP = PBEDataProvider.Instance.GetEXPRequired(pData.GrowthRate, Level);
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
        private void CalcStatsAndAdjustHP(IPBEReadOnlyStatCollection baseStats)
        {
            ushort oldMaxHP = MaxHP;
            CalcStats(baseStats);
            int dif = MaxHP - oldMaxHP;
            HP = (ushort)Math.Max(1, HP + dif);
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

        private void UpdateAbilityAndCalcStatsAfterFormChange()
        {
            var bs = new BaseStats(Species, Form);
            CalcStats(bs.Stats);
            UpdateAbilityIfCannotHave(bs);
        }
        private void UpdateAbilityIfCannotHave(BaseStats bs)
        {
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
            Item = (ItemType)pkmn.Item;
            Ability = pkmn.RevertAbility;
            EffortValues.CopyFrom(pkmn.EffortValues);
            Level = pkmn.Level;
            EXP = pkmn.EXP;
            CalcStats();
            UpdateTimeBasedForms(DateTime.Now);
        }
        public void UpdateFromBattle_Caught(PBEBattlePokemon pkmn)
        {
            SetPlayerOT();
            SetCurrentMetLocation();

            CaughtBall = (ItemType)pkmn.CaughtBall;
            if (CaughtBall == ItemType.FriendBall)
            {
                Friendship = 200;
            }
            else
            {
                var bs = new BaseStats(Species, Form);
                SetDefaultFriendship(bs);
            }

            if (CaughtBall == ItemType.HealBall)
            {
                HealFully();
            }
            UpdateTimeBasedForms(DateTime.Now);
        }

        public void HatchEgg()
        {
            Game.Instance.Save.GameStats[GameStat.HatchedEggs]++;
            IsEgg = false;
            Friendship = PkmnConstants.HatchFriendship;
            SetDefaultNickname();
            SetPlayerOT();
            SetCurrentMetLocation();
            // Burmy hatches as the same form as its mother, but if it was from Mothim & Ditto, it's plant cloak (form 0)
            // Deerling hatches as the current season
            UpdateTimeBasedForms(DateTime.Now);
            CalcStats(); // Calc stats after form is set
            SetMaxHP();
        }
        public void Evolve(EvolutionData.EvoData evo)
        {
            Game.Instance.Save.GameStats[GameStat.EvolvedPokemon]++;
            bool nicknameShouldUpdate = HasDefaultNickname();
            Species = evo.Species;
            Form = evo.Form;
            if (nicknameShouldUpdate)
            {
                SetDefaultNickname();
            }
            var bs = new BaseStats(Species, Form);
            UpdateAbilityIfCannotHave(bs);
            // Calc stats after form is set
            if (HP == 0)
            {
                CalcStats(bs.Stats); // Don't adjust current HP if it's fainted
            }
            else
            {
                CalcStatsAndAdjustHP(bs.Stats);
            }
        }
    }
}
