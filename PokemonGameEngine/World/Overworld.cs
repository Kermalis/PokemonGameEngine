using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.UI;
using Kermalis.PokemonGameEngine.World.Objs;
using System;

namespace Kermalis.PokemonGameEngine.World
{
    internal static partial class Overworld
    {
        public static MapSection GetCurrentLocation()
        {
            return PlayerObj.Player.Map.MapDetails.Section;
        }
        // TODO
        public static bool IsGiratinaLocation()
        {
            return false;
        }
        public static PBEForm GetProperBurmyForm()
        {
            return PlayerObj.Player.Map.MapDetails.BurmyForm;
        }
        public static PBEForm GetProperDeerlingSawsbuckForm()
        {
            DateTime time = Program.LogicTickTime;
            Month month = OverworldTime.GetMonth((Month)time.Month);
            Season season = OverworldTime.GetSeason(month);
            return season.ToDeerlingSawsbuckForm();
        }

        public static bool ShouldRenderDayTint()
        {
            return CameraObj.Camera.Map.MapDetails.Flags.HasFlag(MapFlags.DayTint);
        }
        public static PBEWeather GetPBEWeatherFromMap(MapWeather mapWeather)
        {
            switch (mapWeather)
            {
                case MapWeather.Drought: return PBEWeather.HarshSunlight;
                case MapWeather.Rain_Light:
                case MapWeather.Rain_Medium: return PBEWeather.Rain;
                case MapWeather.Sandstorm: return PBEWeather.Sandstorm;
                case MapWeather.Snow_Hail: return PBEWeather.Hailstorm;
            }
            return PBEWeather.None;
        }
        public static PBEBattleTerrain GetPBEBattleTerrainFromBlock(BlocksetBlockBehavior behavior)
        {
            switch (behavior)
            {
                // Cave
                case BlocksetBlockBehavior.AllowElevationChange_Cave_Encounter:
                case BlocksetBlockBehavior.Cave_Encounter: return PBEBattleTerrain.Cave;
                // Grass
                case BlocksetBlockBehavior.Grass_Encounter:
                case BlocksetBlockBehavior.Grass_SpecialEncounter: return PBEBattleTerrain.Grass;
                // Water
                case BlocksetBlockBehavior.Surf: return PBEBattleTerrain.Water;
            }
            return PBEBattleTerrain.Plain;
        }

        // Returns true if the behavior is a stair (but not a sideways stair)
        public static bool AllowsElevationChange(BlocksetBlockBehavior behavior)
        {
            switch (behavior)
            {
                case BlocksetBlockBehavior.AllowElevationChange:
                case BlocksetBlockBehavior.AllowElevationChange_Cave_Encounter: return true;
            }
            return false;
        }

        public static void DoEnteredMapThings(Map map)
        {
#if DEBUG
            Console.WriteLine("Player is now on {0}", map.Name);
#endif
            SoundControl.SetOverworldBGM(map.MapDetails.Music);
            UpdateGiratinaForms();
        }
        public static void UpdateGiratinaForms()
        {
            foreach (PartyPokemon pkmn in Game.Instance.Save.PlayerParty)
            {
                pkmn.UpdateGiratinaForm();
            }
        }
    }
}
