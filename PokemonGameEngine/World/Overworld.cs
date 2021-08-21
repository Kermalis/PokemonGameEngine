using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.World.Maps;
using Kermalis.PokemonGameEngine.World.Objs;
using System;

namespace Kermalis.PokemonGameEngine.World
{
    internal static partial class Overworld
    {
        public const string SurfScript = "Surf_Interaction";

        public static MapSection GetCurrentLocation()
        {
            return PlayerObj.Player.Map.Details.Section;
        }
        // TODO
        public static bool IsGiratinaLocation()
        {
            return false;
        }
        public static PBEForm GetProperBurmyForm()
        {
            return PlayerObj.Player.Map.Details.BurmyForm;
        }
        public static PBEForm GetProperDeerlingSawsbuckForm()
        {
            DateTime time = Game.LogicTickTime;
            Month month = OverworldTime.GetMonth((Month)time.Month);
            Season season = OverworldTime.GetSeason(month);
            return season.ToDeerlingSawsbuckForm();
        }

        public static bool ShouldRenderDayTint()
        {
            return CameraObj.Camera.Map.Details.Flags.HasFlag(MapFlags.DayTint);
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
        public static bool IsSurfable(BlocksetBlockBehavior behavior)
        {
            switch (behavior)
            {
                case BlocksetBlockBehavior.Surf:
                case BlocksetBlockBehavior.Waterfall: return true;
            }
            return false;
        }
        public static string GetBlockBehaviorScript(BlocksetBlockBehavior behavior)
        {
            switch (behavior)
            {
                case BlocksetBlockBehavior.Surf: return SurfScript;
            }
            return null;
        }

        public static void DoEnteredMapThings(Map map)
        {
#if DEBUG
            Console.WriteLine("Player is now on {0}", map.Name);
#endif
            SoundControl.SetOverworldBGM(map.Details.Music);
            UpdateGiratinaForms();
        }
        public static void UpdateGiratinaForms()
        {
            foreach (PartyPokemon pkmn in Engine.Instance.Save.PlayerParty)
            {
                pkmn.UpdateGiratinaForm();
            }
        }

        public static bool GetNonEggPartyMonWithMove(PBEMove move, out PartyPokemon pkmn, out int index)
        {
            Party party = Engine.Instance.Save.PlayerParty;
            for (int i = 0; i < party.Count; i++)
            {
                PartyPokemon p = party[i];
                if (!p.IsEgg && p.Moveset.Contains(move))
                {
                    pkmn = p;
                    index = i;
                    return true;
                }
            }
            pkmn = null;
            index = -1;
            return false;
        }

        #region Movement

        public static void MoveCoords(FacingDirection dir, int x, int y, out int outX, out int outY)
        {
            switch (dir)
            {
                case FacingDirection.South:
                {
                    outX = x;
                    outY = y + 1;
                    break;
                }
                case FacingDirection.North:
                {
                    outX = x;
                    outY = y - 1;
                    break;
                }
                case FacingDirection.West:
                {
                    outX = x - 1;
                    outY = y;
                    break;
                }
                case FacingDirection.East:
                {
                    outX = x + 1;
                    outY = y;
                    break;
                }
                case FacingDirection.Southwest:
                {
                    outX = x - 1;
                    outY = y + 1;
                    break;
                }
                case FacingDirection.Southeast:
                {
                    outX = x + 1;
                    outY = y + 1;
                    break;
                }
                case FacingDirection.Northwest:
                {
                    outX = x - 1;
                    outY = y - 1;
                    break;
                }
                case FacingDirection.Northeast:
                {
                    outX = x + 1;
                    outY = y - 1;
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(dir));
            }
        }
        public static byte GetElevationIfMovedTo(byte curElevation, byte targetElevations)
        {
            if (!targetElevations.HasElevation(curElevation))
            {
                return targetElevations.GetLowestElevation();
            }
            return curElevation;
        }

        #endregion
    }
}
