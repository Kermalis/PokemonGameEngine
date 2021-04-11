using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.World;
using System;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class BattleEngineDataProvider : PBEDataProvider
    {
        public static new BattleEngineDataProvider Instance => (BattleEngineDataProvider)PBEDataProvider.Instance;

        private bool _isDarkGrass;
        private bool _isCave;
        private bool _isFishing;
        private bool _isSurfing;
        private bool _isUnderwater;

        public void UpdateBattleSetting(bool isCave, bool isDarkGrass, bool isFishing, bool isSurfing, bool isUnderwater)
        {
            _isCave = isCave;
            _isDarkGrass = isDarkGrass;
            _isFishing = isFishing;
            _isSurfing = isSurfing;
            _isUnderwater = isUnderwater;
        }

        public override int GetSpeciesCaught()
        {
            return Game.Instance.Save.Pokedex.GetSpeciesCaught();
        }
        public override bool IsRepeatBallSpecies(PBESpecies species)
        {
            return Game.Instance.Save.Pokedex.IsCaught(species);
        }

        public override bool IsDarkGrass(PBEBattle battle)
        {
            return _isDarkGrass;
        }
        public override bool IsDuskBallSetting(PBEBattle battle)
        {
            if (_isCave)
            {
                return true;
            }
            DateTime time = DateTime.Now;
            Month month = OverworldTime.GetMonth((Month)time.Month);
            Season season = OverworldTime.GetSeason(month);
            int hour = OverworldTime.GetHour(time.Hour);
            return OverworldTime.GetTimeOfDay(season, hour) == TimeOfDay.Night;
        }
        public override bool IsFishing(PBEBattle battle)
        {
            return _isFishing;
        }
        public override bool IsSurfing(PBEBattle battle)
        {
            return _isSurfing;
        }
        public override bool IsUnderwater(PBEBattle battle)
        {
            return _isUnderwater;
        }

        public override bool IsMoonBallFamily(PBESpecies species, PBEForm form)
        {
            bool Check(EvolutionData data)
            {
                foreach (EvolutionData.EvoData e in data.Evolutions)
                {
                    if (e.Method == EvoMethod.Stone && e.Param == (ushort)PBEItem.MoonStone)
                    {
                        return true;
                    }
                }
                return false;
            }

            // Check if this species and its future evolutions evolve by moon stone
            var inData = new EvolutionData(species, form);
            if (Check(inData))
            {
                return true;
            }
            // Check if baby species is a prior evolution and that prior evolution evolves by moon stone
            var babyData = new EvolutionData(inData.BabySpecies, 0);
            if (babyData.IsSpeciesFutureEvo(species) && Check(babyData))
            {
                return true;
            }
            return false;
        }
        public override bool HasEvolutions(PBESpecies species, PBEForm form, bool cache = true)
        {
            return new EvolutionData(species, form).Evolutions.Length > 0;
        }
        public override IPBEPokemonData GetPokemonData(PBESpecies species, PBEForm form, bool cache = true)
        {
            return new BaseStats(species, form);
        }
        public override IPBEPokemonDataExtended GetPokemonDataExtended(PBESpecies species, PBEForm form, bool cache = true)
        {
            throw new InvalidOperationException(); // By default I won't use these systems
        }
    }
}
