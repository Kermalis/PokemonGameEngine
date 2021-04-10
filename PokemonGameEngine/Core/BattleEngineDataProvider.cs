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

        // TODO: Moon ball

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

        /*public override bool HasEvolutions(PBESpecies species, PBEForm form, bool cache = true)
        {
            return base.HasEvolutions(species, form, cache);
        }*/
        public override IPBEPokemonData GetPokemonData(PBESpecies species, PBEForm form, bool cache = true)
        {
            return new BaseStats(species, form);
        }
        /*public override IPBEPokemonDataExtended GetPokemonDataExtended(PBESpecies species, PBEForm form, bool cache = true)
        {
            throw new NotImplementedException(); // Never allow
        }*/
    }
}
