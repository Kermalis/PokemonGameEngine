using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.World;
using System;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class BattleEngineDataProvider : PBEDataProvider
    {
        // TODO: Species caught, moon ball, repeat ball

        public override bool IsDarkGrass(PBEBattle battle)
        {
            return Game.Instance.BattleGUI.IsDarkGrass;
        }
        public override bool IsDuskBallSetting(PBEBattle battle)
        {
            if (Game.Instance.BattleGUI.IsCave)
            {
                return true;
            }
            DateTime time = DateTime.Now;
            return OverworldTime.GetTimeOfDay(OverworldTime.GetSeason(OverworldTime.GetMonth((Month)time.Month)), OverworldTime.GetHour(time.Hour)) == TimeOfDay.Night;
        }
        public override bool IsFishing(PBEBattle battle)
        {
            return Game.Instance.BattleGUI.IsFishing;
        }
        public override bool IsSurfing(PBEBattle battle)
        {
            return Game.Instance.BattleGUI.IsSurfing;
        }
        public override bool IsUnderwater(PBEBattle battle)
        {
            return Game.Instance.BattleGUI.IsUnderwater;
        }
    }
}
