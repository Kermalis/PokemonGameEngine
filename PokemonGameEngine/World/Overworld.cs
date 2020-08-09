using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.World.Objs;
using System;

namespace Kermalis.PokemonGameEngine.World
{
    internal static partial class Overworld
    {
        private static EncounterTable.Encounter RollEncounter(EncounterTable tbl, ushort combinedChance)
        {
            int r = PBEUtils.GlobalRandom.RandomInt(1, combinedChance);
            int sum = 0;
            foreach (EncounterTable.Encounter encounter in tbl.Encounters)
            {
                sum += encounter.Chance;
                if (r <= sum)
                {
                    return encounter;
                }
            }
            throw new Exception("Miscalculation with encounter table data");
        }
        // TODO: Biking lowers the rate by 20% according to gen 3, running does not affect (according to gen 3, maybe it does after)
        public static bool CheckForWildBattle(bool ignoreAbilityOrItem)
        {
            PlayerObj player = PlayerObj.Player;
            Map.Layout.Block block = player.GetBlock(out Map map);
            EncounterType t;
            switch (block.BlocksetBlock.Behavior)
            {
                case BlocksetBlockBehavior.AllowElevationChange_Cave_Encounter:
                case BlocksetBlockBehavior.Cave_Encounter:
                case BlocksetBlockBehavior.Grass_Encounter: t = EncounterType.Default; break;
                case BlocksetBlockBehavior.Grass_SpecialEncounter: t = EncounterType.DarkGrass; break;
                case BlocksetBlockBehavior.Surf: t = EncounterType.Surf; break;
                default: return false;
            }
            EncounterTable tbl = map.Encounters.GetEncounterTable(t);
            if (tbl is null)
            {
                return false;
            }
            int chance = tbl.ChanceOfPhenomenon;
            // Some abilities affect the wild encounter rate
            // This is an option because some encounters (like rock smash) do not use the ability to modify the rate
            if (!ignoreAbilityOrItem)
            {
                PartyPokemon pkmn = Game.Instance.Save.PlayerParty[0];
                PBEAbility abilityOfFirstInParty = pkmn.Ability;
                PBEItem itemOfFirstInParty = pkmn.Item;
                // TODO: CompoundEyes
                // TODO: CuteCharm
                // TODO: Hustle, Pressure, VitalSpirit
                // TODO: Intimidate, KeenEye
                // TODO: MagnetPull, Static
                // TODO: SandVeil, SnowCloak
                // TODO: StickyHold, SuctionCups
                // TODO: Synchronize
                switch (abilityOfFirstInParty)
                {
                    case PBEAbility.ArenaTrap:
                    case PBEAbility.Illuminate:
                    case PBEAbility.NoGuard: chance *= 2; break;
                    case PBEAbility.QuickFeet:
                    case PBEAbility.Stench:
                    case PBEAbility.WhiteSmoke: chance /= 2; break;
                }
                switch (itemOfFirstInParty)
                {
                    case PBEItem.CleanseTag: chance = chance * 2 / 3; break; // Reduce by 1/3
                }
            }
            if (!PBEUtils.GlobalRandom.RandomBool(chance, byte.MaxValue))
            {
                return false;
            }
            ushort combinedChance = tbl.GetCombinedChance();
            if (combinedChance == 0)
            {
                return false;
            }
            EncounterTable.Encounter encounter0 = RollEncounter(tbl, combinedChance);
            Game.Instance.TempCreateWildBattle(map, block, encounter0);
            return true;
        }

        public static bool ShouldRenderDayTint()
        {
            return CameraObj.Camera.Map.MapDetails.Flags.HasFlag(MapFlags.DayTint);
        }
        public static PBEWeather GetPBEWeatherFromMap(Map map)
        {
            MapWeather weather = map.MapDetails.Weather;
            switch (weather)
            {
                case MapWeather.Drought: return PBEWeather.HarshSunlight;
                case MapWeather.Rain_Light:
                case MapWeather.Rain_Medium: return PBEWeather.Rain;
                case MapWeather.Sandstorm: return PBEWeather.Sandstorm;
                case MapWeather.Snow_Hail: return PBEWeather.Hailstorm;
                default: return PBEWeather.None;
            }
        }
        public static PBEBattleTerrain GetPBEBattleTerrainFromBlock(Blockset.Block block)
        {
            BlocksetBlockBehavior behavior = block.Behavior;
            switch (behavior)
            {
                case BlocksetBlockBehavior.AllowElevationChange_Cave_Encounter:
                case BlocksetBlockBehavior.Cave_Encounter: return PBEBattleTerrain.Cave;
                case BlocksetBlockBehavior.Grass_Encounter:
                case BlocksetBlockBehavior.Grass_SpecialEncounter: return PBEBattleTerrain.Grass;
                case BlocksetBlockBehavior.Surf: return PBEBattleTerrain.Water;
                default: return PBEBattleTerrain.Plain;
            }
        }

        // Returns true if the behavior is a stair (but not a sideways stair)
        public static bool AllowsElevationChange(BlocksetBlockBehavior b)
        {
            switch (b)
            {
                case BlocksetBlockBehavior.AllowElevationChange:
                case BlocksetBlockBehavior.AllowElevationChange_Cave_Encounter: return true;
                default: return false;
            }
        }
    }
}
