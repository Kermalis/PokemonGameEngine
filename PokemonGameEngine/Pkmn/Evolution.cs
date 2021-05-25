using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.UI;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal static class Evolution
    {
        private static readonly Queue<(PartyPokemon, EvolutionData.EvoData)> _pendingEvolutions = new(PkmnConstants.PartyCapacity);

        public static void AddPendingEvolution(PartyPokemon pkmn, EvolutionData.EvoData evo)
        {
            _pendingEvolutions.Enqueue((pkmn, evo));
        }
        public static bool GetNextPendingEvolution(out (PartyPokemon, EvolutionData.EvoData) evo)
        {
            return _pendingEvolutions.TryDequeue(out evo);
        }

        private static bool IsEverstone(PartyPokemon pkmn)
        {
            return pkmn.Item == ItemType.Everstone;
        }
        private static bool IsNight()
        {
            DateTime time = Program.LogicTickTime;
            Month month = OverworldTime.GetMonth((Month)time.Month);
            Season season = OverworldTime.GetSeason(month);
            int hour = OverworldTime.GetHour(time.Hour);
            return OverworldTime.GetTimeOfDay(season, hour) == TimeOfDay.Night;
        }
        private static bool IsNosepassMagnetonLocation()
        {
            MapSection mapSection = Overworld.GetCurrentLocation();
            return mapSection == MapSection.TestCave;
        }
        // TODO
        private static bool IsLeafeonLocation()
        {
            return false;
        }
        // TODO
        private static bool IsGlaceonLocation()
        {
            return false;
        }

        // Ignores Shedinja_LevelUp & Beauty_LevelUp
        public static EvolutionData.EvoData GetLevelUpEvolution(Party party, PartyPokemon pkmn)
        {
            if (IsEverstone(pkmn))
            {
                return null;
            }

            bool isNight = IsNight();

            var data = new EvolutionData(pkmn.Species, pkmn.Form);
            foreach (EvolutionData.EvoData evo in data.Evolutions)
            {
                bool isMatch;
                switch (evo.Method)
                {
                    case EvoMethod.Friendship_LevelUp: isMatch = pkmn.Friendship >= evo.Param; break;
                    case EvoMethod.Friendship_Day_LevelUp: isMatch = !isNight && pkmn.Friendship >= evo.Param; break;
                    case EvoMethod.Friendship_Night_LevelUp: isMatch = isNight && pkmn.Friendship >= evo.Param; break;
                    case EvoMethod.LevelUp:
                    case EvoMethod.Ninjask_LevelUp: isMatch = pkmn.Level >= evo.Param; break;
                    case EvoMethod.ATK_GT_DEF_LevelUp: isMatch = pkmn.Level >= evo.Param && pkmn.Attack > pkmn.Defense; break;
                    case EvoMethod.ATK_EE_DEF_LevelUp: isMatch = pkmn.Level >= evo.Param && pkmn.Attack == pkmn.Defense; break;
                    case EvoMethod.ATK_LT_DEF_LevelUp: isMatch = pkmn.Level >= evo.Param && pkmn.Attack < pkmn.Defense; break;
                    case EvoMethod.Silcoon_LevelUp: isMatch = pkmn.Level >= evo.Param && ((pkmn.PID >> 0x10) % 10) <= 4; break;
                    case EvoMethod.Cascoon_LevelUp: isMatch = pkmn.Level >= evo.Param && ((pkmn.PID >> 0x10) % 10) > 4; break;
                    case EvoMethod.Item_Day_LevelUp: isMatch = !isNight && pkmn.Item == (ItemType)evo.Param; break;
                    case EvoMethod.Item_Night_LevelUp: isMatch = isNight && pkmn.Item == (ItemType)evo.Param; break;
                    case EvoMethod.Move_LevelUp: isMatch = pkmn.Moveset.Contains((PBEMove)evo.Param); break;
                    case EvoMethod.Male_LevelUp: isMatch = pkmn.Level >= evo.Param && pkmn.Gender == PBEGender.Male; break;
                    case EvoMethod.Female_LevelUp: isMatch = pkmn.Level >= evo.Param && pkmn.Gender == PBEGender.Female; break;
                    case EvoMethod.NosepassMagneton_Location_LevelUp: isMatch = IsNosepassMagnetonLocation(); break;
                    case EvoMethod.Leafeon_Location_LevelUp: isMatch = IsLeafeonLocation(); break;
                    case EvoMethod.Glaceon_Location_LevelUp: isMatch = IsGlaceonLocation(); break;
                    case EvoMethod.PartySpecies_LevelUp:
                    {
                        isMatch = false;
                        foreach (PartyPokemon p in party)
                        {
                            if (p != pkmn && !p.IsEgg && p.Species == (PBESpecies)evo.Param)
                            {
                                isMatch = true;
                                break;
                            }
                        }
                        break;
                    }
                    default: isMatch = false; break;
                }
                if (isMatch)
                {
                    return evo;
                }
            }
            return null;
        }

        public static EvolutionData.EvoData GetItemEvolution(PartyPokemon pkmn, ItemType item)
        {
            bool isNight = IsNight();

            var data = new EvolutionData(pkmn.Species, pkmn.Form);
            foreach (EvolutionData.EvoData evo in data.Evolutions)
            {
                if (item != (ItemType)evo.Param)
                {
                    continue;
                }
                bool isMatch;
                switch (evo.Method)
                {
                    case EvoMethod.Stone: isMatch = true; break;
                    case EvoMethod.Male_Stone: isMatch = pkmn.Gender == PBEGender.Male; break;
                    case EvoMethod.Female_Stone: isMatch = pkmn.Gender == PBEGender.Female; break;
                    case EvoMethod.Item_Day_LevelUp: isMatch = !isNight; break;
                    case EvoMethod.Item_Night_LevelUp: isMatch = isNight; break;
                    default: isMatch = false; break;
                }
                if (isMatch)
                {
                    return evo;
                }
            }
            return null;
        }

        public static EvolutionData.EvoData GetTradeEvolution(PartyPokemon pkmn, PBESpecies otherSpecies)
        {
            if (IsEverstone(pkmn))
            {
                return null;
            }

            var data = new EvolutionData(pkmn.Species, pkmn.Form);
            foreach (EvolutionData.EvoData evo in data.Evolutions)
            {
                bool isMatch;
                switch (evo.Method)
                {
                    case EvoMethod.Trade: isMatch = true; break;
                    case EvoMethod.Item_Trade: isMatch = pkmn.Item == (ItemType)evo.Param; break;
                    case EvoMethod.ShelmetKarrablast:
                    {
                        isMatch = (pkmn.Species == PBESpecies.Shelmet && otherSpecies == PBESpecies.Karrablast)
                            || (pkmn.Species == PBESpecies.Karrablast && otherSpecies == PBESpecies.Shelmet);
                        break;
                    }
                    default: isMatch = false; break;
                }
                if (isMatch)
                {
                    return evo;
                }
            }
            return null;
        }

        // Only level-up evolutions can be cancelled
        public static bool CanCancelEvolution(EvoMethod method)
        {
            switch (method)
            {
                case EvoMethod.Friendship_LevelUp:
                case EvoMethod.Friendship_Day_LevelUp:
                case EvoMethod.Friendship_Night_LevelUp:
                case EvoMethod.LevelUp:
                case EvoMethod.ATK_GT_DEF_LevelUp:
                case EvoMethod.ATK_EE_DEF_LevelUp:
                case EvoMethod.ATK_LT_DEF_LevelUp:
                case EvoMethod.Silcoon_LevelUp:
                case EvoMethod.Cascoon_LevelUp:
                case EvoMethod.Ninjask_LevelUp:
                case EvoMethod.Shedinja_LevelUp:
                case EvoMethod.Beauty_LevelUp:
                case EvoMethod.Item_Day_LevelUp:
                case EvoMethod.Item_Night_LevelUp:
                case EvoMethod.Move_LevelUp:
                case EvoMethod.PartySpecies_LevelUp:
                case EvoMethod.Male_LevelUp:
                case EvoMethod.Female_LevelUp:
                case EvoMethod.NosepassMagneton_Location_LevelUp:
                case EvoMethod.Leafeon_Location_LevelUp:
                case EvoMethod.Glaceon_Location_LevelUp: return true;
            }
            return false;
        }

        public static void TryCreateShedinja(PartyPokemon nincada)
        {
            Party party = Game.Instance.Save.PlayerParty;
            if (party.Count >= PkmnConstants.PartyCapacity)
            {
                return;
            }

            Inventory<InventorySlotNew> inv = Game.Instance.Save.PlayerInventory;
            if (!inv.TryRemove(ItemType.PokeBall, 1))
            {
                return;
            }

            var shedinja = PartyPokemon.CreateShedinja(nincada);
            party.Add(shedinja);
            Game.Instance.Save.Pokedex.SetCaught(shedinja.Species, shedinja.Form, shedinja.Gender, shedinja.Shiny, shedinja.PID);
        }
    }
}
