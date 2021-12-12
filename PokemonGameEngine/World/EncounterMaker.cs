using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.World.Data;
using Kermalis.PokemonGameEngine.World.Maps;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World
{
    // TODO: CompoundEyes
    // TODO: StickyHold, SuctionCups
    internal static class EncounterMaker
    {
        private static bool TryGetEncounterType(BlocksetBlockBehavior behavior, out EncounterType type)
        {
            switch (behavior)
            {
                // Default
                case BlocksetBlockBehavior.AllowElevationChange_Cave_Encounter:
                case BlocksetBlockBehavior.Cave_Encounter:
                case BlocksetBlockBehavior.Grass_Encounter:
                    type = EncounterType.Default; return true;
                // DarkGrass
                case BlocksetBlockBehavior.Grass_SpecialEncounter:
                    type = EncounterType.DarkGrass; return true;
                // Surf
                case BlocksetBlockBehavior.Surf:
                    type = EncounterType.Surf; return true;
            }
            type = default;
            return false;
        }

        private static Song GetWildBattleMusic(Party wildParty)
        {
            if (wildParty.Count != 1)
            {
                return Song.WildBattle_Multi;
            }
            // All legendary/mythical species use the legendary battle music
            PBESpecies s = wildParty[0].Species;
            switch (s)
            {
                case PBESpecies.Articuno:
                case PBESpecies.Zapdos:
                case PBESpecies.Moltres:
                case PBESpecies.Mewtwo:
                case PBESpecies.Mew:
                case PBESpecies.Raikou:
                case PBESpecies.Entei:
                case PBESpecies.Suicune:
                case PBESpecies.Lugia:
                case PBESpecies.HoOh:
                case PBESpecies.Celebi:
                case PBESpecies.Regirock:
                case PBESpecies.Regice:
                case PBESpecies.Registeel:
                case PBESpecies.Latias:
                case PBESpecies.Latios:
                case PBESpecies.Kyogre:
                case PBESpecies.Groudon:
                case PBESpecies.Rayquaza:
                case PBESpecies.Jirachi:
                case PBESpecies.Deoxys:
                case PBESpecies.Uxie:
                case PBESpecies.Mesprit:
                case PBESpecies.Azelf:
                case PBESpecies.Dialga:
                case PBESpecies.Palkia:
                case PBESpecies.Heatran:
                case PBESpecies.Regigigas:
                case PBESpecies.Giratina:
                case PBESpecies.Cresselia:
                case PBESpecies.Phione:
                case PBESpecies.Manaphy:
                case PBESpecies.Darkrai:
                case PBESpecies.Shaymin:
                case PBESpecies.Arceus:
                case PBESpecies.Victini:
                case PBESpecies.Cobalion:
                case PBESpecies.Terrakion:
                case PBESpecies.Virizion:
                case PBESpecies.Tornadus:
                case PBESpecies.Thundurus:
                case PBESpecies.Reshiram:
                case PBESpecies.Zekrom:
                case PBESpecies.Landorus:
                case PBESpecies.Kyurem:
                case PBESpecies.Keldeo:
                case PBESpecies.Meloetta:
                case PBESpecies.Genesect:
                    return Song.LegendaryBattle;
            }
            return Song.WildBattle;
        }

        private static EncounterTable.Encounter RollEncounter(IEnumerable<EncounterTable.Encounter> tbl, ushort combinedChance)
        {
            int r = PBEDataProvider.GlobalRandom.RandomInt(1, combinedChance);
            int sum = 0;
            foreach (EncounterTable.Encounter encounter in tbl)
            {
                sum += encounter.Chance;
                if (r <= sum)
                {
                    return encounter;
                }
            }
            throw new Exception("Miscalculation with encounter table data");
        }
        private static EncounterTable.Encounter RollEncounterOfTypeIfPossible(EncounterTable.Encounter[] tbl, ushort combinedChance, PBEType type)
        {
            var typeEncounters = new List<EncounterTable.Encounter>(tbl.Length);
            ushort typeChance = 0;

            foreach (EncounterTable.Encounter encounter in tbl)
            {
                var bs = BaseStats.Get(encounter.Species, encounter.Form, false);
                if (bs.HasType(type))
                {
                    typeEncounters.Add(encounter);
                    typeChance += encounter.Chance;
                }
            }

            if (typeEncounters.Count == 0)
            {
                return RollEncounter(tbl, combinedChance);
            }
            if (typeChance == 0)
            {
                return null;
            }
            return RollEncounter(typeEncounters, typeChance);
        }

        // Some abilities & items affect the wild encounter rate
        // Biking lowers the rate by 20% (except for when using Rock Smash)
        private static int GetAffectedChance(int chance, PBEAbility leadPkmnAbility, ItemType leadPkmnItem, bool isBiking, MapWeather weather)
        {
            switch (leadPkmnAbility)
            {
                case PBEAbility.ArenaTrap:
                case PBEAbility.Illuminate:
                case PBEAbility.NoGuard: chance *= 2; break;
                case PBEAbility.QuickFeet:
                case PBEAbility.Stench:
                case PBEAbility.WhiteSmoke: chance /= 2; break;
                case PBEAbility.SandVeil:
                {
                    if (weather == MapWeather.Sandstorm)
                    {
                        chance /= 2;
                    }
                    break;
                }
                case PBEAbility.SnowCloak:
                {
                    if (weather == MapWeather.Snow_Hail)
                    {
                        chance /= 2;
                    }
                    break;
                }
            }
            switch (leadPkmnItem)
            {
                case ItemType.CleanseTag: chance = chance * 2 / 3; break; // Reduce by 1/3
            }
            if (isBiking)
            {
                chance = chance * 4 / 5; // Reduce by 1/5
            }
            return chance;
        }

        private static EncounterTable.Encounter GetAffectedEncounter(PBEAbility leadPkmnAbility, EncounterTable.Encounter[] tbl, ushort combinedChance)
        {
            // MagnetPull and Static have a 50% chance to activate
            if (PBEDataProvider.GlobalRandom.RandomBool())
            {
                switch (leadPkmnAbility)
                {
                    case PBEAbility.MagnetPull: return RollEncounterOfTypeIfPossible(tbl, combinedChance, PBEType.Steel);
                    case PBEAbility.Static: return RollEncounterOfTypeIfPossible(tbl, combinedChance, PBEType.Electric);
                }
            }
            return RollEncounter(tbl, combinedChance);
        }
        private static bool ShouldCancelEncounter(PBEAbility leadPkmnAbility, byte leadPkmnLevel, byte encounterLevel)
        {
            // Intimidate and KeenEye have a 50% chance to cancel the encounter if the level would be 5 or more levels below
            switch (leadPkmnAbility)
            {
                case PBEAbility.Intimidate:
                case PBEAbility.KeenEye:
                {
                    if (leadPkmnLevel >= encounterLevel + 5 && PBEDataProvider.GlobalRandom.RandomBool())
                    {
                        return true;
                    }
                    break;
                }
            }
            return false;
        }
        private static PBEForm GetAffectedForm(PBESpecies species, PBEForm form, EncounterType type)
        {
            switch (species)
            {
                // Burmy is always Plant cloak if it's from a honey tree
                case PBESpecies.Burmy: return type == EncounterType.HoneyTree ? PBEForm.Burmy_Plant : Overworld.GetProperBurmyForm();
                case PBESpecies.Deerling:
                case PBESpecies.Sawsbuck: return Overworld.GetProperDeerlingSawsbuckForm();
                case PBESpecies.Giratina: return Overworld.IsGiratinaLocation() ? PBEForm.Giratina_Origin : PBEForm.Giratina;
            }
            return form;
        }
        private static byte GetAffectedLevel(PBEAbility leadPkmnAbility, EncounterTable.Encounter encounter)
        {
            // Hustle, Pressure, and VitalSpirit have a 50% chance to make the encounter max level
            switch (leadPkmnAbility)
            {
                case PBEAbility.Hustle:
                case PBEAbility.Pressure:
                case PBEAbility.VitalSpirit:
                {
                    if (PBEDataProvider.GlobalRandom.RandomBool())
                    {
                        return encounter.MaxLevel;
                    }
                    break;
                }
            }
            // Return random level
            return (byte)PBEDataProvider.GlobalRandom.RandomInt(encounter.MinLevel, encounter.MaxLevel);
        }
        private static PBEGender GetAffectedGender(PBEGender leadPkmnGender, PBEAbility leadPkmnAbility, PBEGenderRatio encounterGenderRatio)
        {
            // CuteCharm has a 66.6~% chance to force the encounter to be opposite gender
            if (leadPkmnGender != PBEGender.Genderless && encounterGenderRatio != PBEGenderRatio.M0_F0
                && leadPkmnAbility == PBEAbility.CuteCharm && PBEDataProvider.GlobalRandom.RandomBool(2, 3))
            {
                if (leadPkmnGender == PBEGender.Male)
                {
                    if (encounterGenderRatio != PBEGenderRatio.M1_F0)
                    {
                        return PBEGender.Female;
                    }
                }
                else
                {
                    if (encounterGenderRatio != PBEGenderRatio.M0_F1)
                    {
                        return PBEGender.Male;
                    }
                }
            }
            return PBEDataProvider.GlobalRandom.RandomGender(encounterGenderRatio);
        }
        // Does not apply to roaming mon
        private static PBENature GetAffectedNature(PBEAbility leadPkmnAbility, PBENature leadPkmnNature)
        {
            // Synchronize has a 50% chance to force the same nature
            if (leadPkmnAbility == PBEAbility.Synchronize && PBEDataProvider.GlobalRandom.RandomBool())
            {
                return leadPkmnNature;
            }
            return PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.AllNatures);
        }

        private static void GetWildBattleFormat(EncounterType t, out int numWild, out PBEBattleFormat bf)
        {
            if (t == EncounterType.DarkGrass)
            {
                numWild = 2;
                bf = PBEBattleFormat.Double;
            }
            else
            {
                numWild = 1;
                bf = PBEBattleFormat.Single;
            }
        }

        private static bool CreateWildPkmn(EncounterType type, EncounterTable.Encounter[] tbl, ushort combinedChance, PartyPokemon leadPkmn, Party wildParty)
        {
            EncounterTable.Encounter encounter = GetAffectedEncounter(leadPkmn.Ability, tbl, combinedChance);
            if (encounter is null)
            {
                return false;
            }
            byte level = GetAffectedLevel(leadPkmn.Ability, encounter);
            // Check if we should cancel the encounter
            if (ShouldCancelEncounter(leadPkmn.Ability, leadPkmn.Level, level))
            {
                return false;
            }
            PBESpecies species = encounter.Species;
            PBEForm form = GetAffectedForm(species, encounter.Form, type);
            var bs = BaseStats.Get(species, form, true);
            PBEGender gender = GetAffectedGender(leadPkmn.Gender, leadPkmn.Ability, bs.GenderRatio);
            PBENature nature = GetAffectedNature(leadPkmn.Ability, leadPkmn.Nature);
            wildParty.Add(PartyPokemon.CreateWildMon(species, form, level, gender, nature, bs));
            return true;
        }

        public static bool CheckForWildBattle(bool ignoreAbilityOrItemOrBike)
        {
            PlayerObj player = PlayerObj.Instance;
            MapLayout.Block block = player.GetBlock();
            BlocksetBlockBehavior blockBehavior = block.BlocksetBlock.Behavior;
            if (!TryGetEncounterType(blockBehavior, out EncounterType t))
            {
                return false; // Return false if the block does not create battles
            }
            Map map = player.Map;
            EncounterTable tbl = map.Encounters.GetEncounterTable(t);
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
            MapWeather weather = map.Details.Weather;
            int chance = tbl.ChanceOfPhenomenon;
            // This is an option because some encounters (like rock smash) do not use the ability to modify the rate
            if (!ignoreAbilityOrItemOrBike)
            {
                chance = GetAffectedChance(chance, leadPkmn.Ability, leadPkmn.Item, player.State == PlayerObjState.Biking, weather);
            }
            if (!PBEDataProvider.GlobalRandom.RandomBool(chance, byte.MaxValue))
            {
                return false; // Return false if the chance was not rolled
            }

            // We passed all the checks, now we get an encounter
            var wildParty = new Party();
            GetWildBattleFormat(t, out int numWild, out PBEBattleFormat format);
            for (int i = 0; i < numWild; i++)
            {
                if (!CreateWildPkmn(t, tbl.Encounters, combinedChance, leadPkmn, wildParty))
                {
                    return false; // Return false if an ability cancels the encounter
                }
            }
            BattleMaker.CreateWildBattle(weather, blockBehavior, wildParty, format, GetWildBattleMusic(wildParty));
            return true;
        }

        public static void CreateStaticWildBattle(PBESpecies species, PBEForm form, byte level)
        {
            PlayerObj player = PlayerObj.Instance;
            MapLayout.Block block = player.GetBlock();
            PartyPokemon leadPkmn = Game.Instance.Save.PlayerParty[0];
            MapWeather weather = player.Map.Details.Weather;
            var bs = BaseStats.Get(species, form, true);
            PBEGender gender = GetAffectedGender(leadPkmn.Gender, leadPkmn.Ability, bs.GenderRatio);
            PBENature nature = GetAffectedNature(leadPkmn.Ability, leadPkmn.Nature);
            var wildPkmn = PartyPokemon.CreateWildMon(species, form, level, gender, nature, bs);
            var wildParty = new Party { wildPkmn };
            BattleMaker.CreateWildBattle(weather, block.BlocksetBlock.Behavior, wildParty, PBEBattleFormat.Single, GetWildBattleMusic(wildParty));
        }
    }
}
