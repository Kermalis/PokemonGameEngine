using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.World.Objs;
using System;

namespace Kermalis.PokemonGameEngine.World
{
    // TODO: CompoundEyes
    // TODO: CuteCharm
    // TODO: Hustle, Pressure, VitalSpirit
    // TODO: Intimidate, KeenEye
    // TODO: MagnetPull, Static
    // TODO: SandVeil, SnowCloak
    // TODO: StickyHold, SuctionCups
    // TODO: Synchronize
    internal static class Encounter
    {
        public static EncounterType? GetEncounterType(BlocksetBlockBehavior b)
        {
            switch (b)
            {
                case BlocksetBlockBehavior.AllowElevationChange_Cave_Encounter:
                case BlocksetBlockBehavior.Cave_Encounter:
                case BlocksetBlockBehavior.Grass_Encounter: return EncounterType.Default;
                case BlocksetBlockBehavior.Grass_SpecialEncounter: return EncounterType.DarkGrass;
                case BlocksetBlockBehavior.Surf: return EncounterType.Surf;
            }
            return null;
        }

        private static EncounterTable.Encounter RollEncounter(EncounterTable tbl, ushort combinedChance)
        {
            int r = PBEDataProvider.GlobalRandom.RandomInt(1, combinedChance);
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

        // Some abilities & items affect the wild encounter rate
        // Biking lowers the rate by 20% (except for when using Rock Smash)
        private static int GetAffectedChance(PartyPokemon leadPkmn, int chance, bool isBiking)
        {
            PBEAbility abilityOfFirstInParty = leadPkmn.Ability;
            ItemType itemOfFirstInParty = leadPkmn.Item;
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
                case ItemType.CleanseTag: chance = chance * 2 / 3; break; // Reduce by 1/3
            }
            if (isBiking)
            {
                chance = chance * 4 / 5; // Reduce by 1/5
            }
            return chance;
        }

        // TODO: Get IsBiking
        public static bool CheckForWildBattle(bool ignoreAbilityOrItemOrBike)
        {
            PlayerObj player = PlayerObj.Player;
            Map.Layout.Block block = player.GetBlock();
            BlocksetBlockBehavior blockBehavior = block.BlocksetBlock.Behavior;
            EncounterType? t = GetEncounterType(blockBehavior);
            if (!t.HasValue)
            {
                return false; // Return false if the block does not create battles
            }
            Map map = player.Map;
            EncounterTable tbl = map.Encounters.GetEncounterTable(t.Value);
            if (tbl is null)
            {
                return false; // Return false if there are no encounters for this block on this map
            }
            ushort combinedChance = tbl.GetCombinedChance();
            if (combinedChance == 0)
            {
                return false; // Return false if all of the encounters are disabled
            }
            PartyPokemon leadPkmn = Game.Instance.Save.PlayerParty[0];
            int chance = tbl.ChanceOfPhenomenon;
            // This is an option because some encounters (like rock smash) do not use the ability to modify the rate
            if (!ignoreAbilityOrItemOrBike)
            {
                chance = GetAffectedChance(leadPkmn, chance, false);
            }
            if (!PBEDataProvider.GlobalRandom.RandomBool(chance, byte.MaxValue))
            {
                return false; // Return false if the chance was not rolled
            }

            // We passed all the checks, now we get an encounter
            EncounterTable.Encounter encounter0 = RollEncounter(tbl, combinedChance);
            Game.Instance.TempCreateWildBattle(map.MapDetails.Weather, blockBehavior, encounter0);
            return true;
        }
    }
}
