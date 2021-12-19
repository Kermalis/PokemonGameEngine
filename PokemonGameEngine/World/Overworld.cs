﻿using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.World.Maps;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
#if DEBUG_OVERWORLD
using Kermalis.PokemonGameEngine.Debug;
#endif

namespace Kermalis.PokemonGameEngine.World
{
    internal static partial class Overworld
    {
        public const string SurfScript = "Surf_Interaction";

        public static MapSection GetCurrentLocation()
        {
            return PlayerObj.Instance.Map.Details.Section;
        }
        // TODO
        public static bool IsGiratinaLocation()
        {
            return false;
        }
        public static PBEForm GetProperBurmyForm()
        {
            return PlayerObj.Instance.Map.Details.BurmyForm;
        }
        public static PBEForm GetProperDeerlingSawsbuckForm()
        {
            DateTime time = DateTime.Now;
            Month month = OverworldTime.GetMonth((Month)time.Month);
            Season season = OverworldTime.GetSeason(month);
            return season.ToDeerlingSawsbuckForm();
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
#if DEBUG_OVERWORLD
            Log.WriteLine("Player is now on  map: " + map.Name);
#endif
            SoundControl.SetOverworldBGM(map.Details.Music);
            UpdateGiratinaForms();
        }
        public static void UpdateGiratinaForms()
        {
            foreach (PartyPokemon pkmn in Game.Instance.Save.PlayerParty)
            {
                pkmn.UpdateGiratinaForm();
            }
        }

        public static bool GetNonEggPartyMonWithMove(PBEMove move, out PartyPokemon pkmn, out int index)
        {
            Party party = Game.Instance.Save.PlayerParty;
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
