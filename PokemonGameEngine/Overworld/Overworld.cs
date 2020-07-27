using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.Pkmn;
using System;

namespace Kermalis.PokemonGameEngine.Overworld
{
    internal static class Overworld
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
            Obj player = Obj.Player;
            Map map = player.Map;
            EncounterType t;
            switch (map.GetBlock(player.X, player.Y).BlocksetBlock.Behavior)
            {
                case BlocksetBlockBehavior.DarkGrass: t = EncounterType.DarkGrass; break;
                case BlocksetBlockBehavior.Surf: t = EncounterType.Surf; break;
                case BlocksetBlockBehavior.WildEncounter: t = EncounterType.Default; break;
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
                PartyPokemon pkmn = Game.Game.Instance.Save.PlayerParty[0];
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
            Game.Game.Instance.TempCreateWildBattle(encounter0);
            return true;
        }
    }
}
