﻿using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Player;
using Kermalis.PokemonGameEngine.Trainer;
using Kermalis.PokemonGameEngine.World;

namespace Kermalis.PokemonGameEngine.Core
{
    internal static class BattleMaker
    {
        private static PBEWeather GetPBEWeather(MapWeather mapWeather)
        {
            switch (mapWeather)
            {
                case MapWeather.Drought:
                    return PBEWeather.HarshSunlight;
                case MapWeather.Rain_Light:
                case MapWeather.Rain_Medium:
                    return PBEWeather.Rain;
                case MapWeather.Sandstorm:
                    return PBEWeather.Sandstorm;
                case MapWeather.Snow_Hail:
                    return PBEWeather.Hailstorm;
            }
            return PBEWeather.None;
        }
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

        private static PBETrainerInfo CreatePlayerInfo(Save sav)
        {
            return new PBETrainerInfo(sav.PlayerParty, sav.OT.TrainerName, true, inventory: sav.PlayerInventory.ToPBEInventory());
        }

        public static void CreateWildBattle(Party wildParty,
            MapWeather mapWeather, BlocksetBlockBehavior blockBehavior,
            PBEBattleFormat format, Song song)
        {
            Save sav = Game.Instance.Save;

            PBETrainerInfo me = CreatePlayerInfo(sav);
            var trainerParties = new Party[] { sav.PlayerParty, wildParty };
            var wild = new PBEWildInfo(wildParty);

            PBEBattleTerrain terrain = UpdateBattleSetting(blockBehavior);
            var battle = PBEBattle.CreateWildBattle(format, PkmnConstants.PBESettings, me, wild,
                battleTerrain: terrain, weather: GetPBEWeather(mapWeather));

            OverworldGUI.Instance.StartWildBattle(battle, song, trainerParties);
            sav.GameStats[GameStat.TotalBattles]++;
            sav.GameStats[GameStat.WildBattles]++;
        }
        public static void CreateTrainerBattle_1v1(PBETrainerInfo enemyInfo, Party[] trainerParties,
            MapWeather mapWeather, BlocksetBlockBehavior blockBehavior,
            PBEBattleFormat format, Song song,
            TrainerClass c, string defeatText)
        {
            Save sav = Game.Instance.Save;

            PBETrainerInfo me = CreatePlayerInfo(sav);

            PBEBattleTerrain terrain = UpdateBattleSetting(blockBehavior);
            var battle = PBEBattle.CreateTrainerBattle(format, PkmnConstants.PBESettings, me, enemyInfo,
                battleTerrain: terrain, weather: GetPBEWeather(mapWeather));

            OverworldGUI.Instance.StartTrainerBattle(battle, song, trainerParties, c, defeatText);
            sav.GameStats[GameStat.TotalBattles]++;
            sav.GameStats[GameStat.TrainerBattles]++;
        }
    }
}
