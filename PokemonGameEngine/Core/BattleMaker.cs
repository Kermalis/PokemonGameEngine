using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Player;
using Kermalis.PokemonGameEngine.Trainer;
using Kermalis.PokemonGameEngine.World;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Core
{
    internal static class BattleMaker
    {
        private static PBEBattleTerrain UpdateBattleSetting(BlocksetBlockBehavior blockBehavior)
        {
            PBEBattleTerrain terrain = Overworld.GetPBEBattleTerrainFromBlock(blockBehavior);
            BattleEngineDataProvider.Instance.UpdateBattleSetting(isCave: terrain == PBEBattleTerrain.Cave,
                isDarkGrass: blockBehavior == BlocksetBlockBehavior.Grass_SpecialEncounter,
                isFishing: false,
                isSurfing: blockBehavior == BlocksetBlockBehavior.Surf,
                isUnderwater: false);
            return terrain;
        }
        private static void CreateBattle(PBEBattle battle, Song song, IReadOnlyList<Party> trainerParties, TrainerClass c = default, string defeatText = null)
        {
            OverworldGUI.Instance.SetupBattle(battle, song, trainerParties, c: c, defeatText: defeatText);
            Game.Instance.Save.GameStats[GameStat.TotalBattles]++;
        }
        public static void CreateWildBattle(MapWeather mapWeather, BlocksetBlockBehavior blockBehavior, Party wildParty, PBEBattleFormat format, Song song)
        {
            Save sav = Game.Instance.Save;
            var me = new PBETrainerInfo(sav.PlayerParty, sav.OT.TrainerName, true, inventory: sav.PlayerInventory.ToPBEInventory());
            var trainerParties = new Party[] { sav.PlayerParty, wildParty };
            var wild = new PBEWildInfo(wildParty);
            PBEBattleTerrain terrain = UpdateBattleSetting(blockBehavior);
            var battle = PBEBattle.CreateWildBattle(format, PkmnConstants.PBESettings, me, wild, battleTerrain: terrain, weather: Overworld.GetPBEWeatherFromMap(mapWeather));
            CreateBattle(battle, song, trainerParties);
            sav.GameStats[GameStat.WildBattles]++;
        }
        public static void CreateTrainerBattle_1v1(MapWeather mapWeather, BlocksetBlockBehavior blockBehavior, Party[] trainerParties, PBETrainerInfo enemyInfo, PBEBattleFormat format, Song song, TrainerClass c, string defeatText)
        {
            Save sav = Game.Instance.Save;
            var me = new PBETrainerInfo(sav.PlayerParty, sav.OT.TrainerName, true, inventory: sav.PlayerInventory.ToPBEInventory());
            PBEBattleTerrain terrain = UpdateBattleSetting(blockBehavior);
            var battle = PBEBattle.CreateTrainerBattle(format, PkmnConstants.PBESettings, me, enemyInfo, battleTerrain: terrain, weather: Overworld.GetPBEWeatherFromMap(mapWeather));
            CreateBattle(battle, song, trainerParties, c: c, defeatText: defeatText);
            sav.GameStats[GameStat.TrainerBattles]++;
        }
    }
}
