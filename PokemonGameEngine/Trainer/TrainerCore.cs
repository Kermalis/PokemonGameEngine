using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Maps;
using Kermalis.PokemonGameEngine.World.Objs;
using System;

namespace Kermalis.PokemonGameEngine.Trainer
{
    internal static class TrainerCore
    {
        public static Song GetTrainerClassSong(TrainerClass c)
        {
            switch (c)
            {
                case TrainerClass.Leader:
                    return Song.BattleGymLeader;
            }
            return Song.BattleTrainer;
        }
        public static string GetTrainerClassAsset(TrainerClass c)
        {
            string s;
            switch (c)
            {
                case TrainerClass.PkmnTrainer: // TODO
                case TrainerClass.Lady: s = "Lady"; break;
                case TrainerClass.Leader: s = "Janine"; break;
                default: throw new ArgumentOutOfRangeException(nameof(c));
            }
            return string.Format("Sprites\\Trainers\\{0}.gif", s);
        }
        public static string GetTrainerClassName(TrainerClass c)
        {
            switch (c)
            {
                case TrainerClass.PkmnTrainer: return "[PK][MN] Trainer";
                case TrainerClass.Lady: return "Lady";
                case TrainerClass.Leader: return "Leader";
            }
            throw new ArgumentOutOfRangeException(nameof(c));
        }

        private static void AddMon(PBESpecies species, PBEForm form, byte level, PBEGender gender, PBENature nature, Party party)
        {
            var p = PartyPokemon.CreateWildMon(species, form, level, gender, nature, BaseStats.Get(species, form, true));
            party.Add(p);
        }
        private static Party Debug_CreateParty(bool temp)
        {
            var ret = new Party();
            if (temp)
            {
                AddMon(PBESpecies.Shaymin, PBEForm.Shaymin_Sky, 20, PBEGender.Genderless, PBENature.Bashful, ret);
            }
            else
            {
                AddMon(PBESpecies.Rayquaza, 0, 50, PBEGender.Genderless, PBENature.Bashful, ret);
            }
            return ret;
        }

        public static void CreateTrainerBattle_1v1(Flag trainer, string defeatText)
        {
            PlayerObj player = PlayerObj.Instance;
            Map map = player.Map;
            MapLayout.Block block = player.GetBlock();
            MapWeather weather = map.Details.Weather;
            BlocksetBlockBehavior behavior = block.BlocksetBlock.Behavior;

            TrainerClass tc = trainer == Flag.Trainer1 ? TrainerClass.PkmnTrainer : TrainerClass.Leader; // TODO
            string name = trainer == Flag.Trainer1 ? "Candice" : "Saw Con"; // TODO
            Party enemyParty = Debug_CreateParty(trainer == Flag.Trainer1); // TODO
            (PBEItem, uint)[] inv = null; // TODO
            PBEBattleFormat format = PBEBattleFormat.Single; // TODO

            var enemyInfo = new PBETrainerInfo(enemyParty, string.Format("{0} {1}", GetTrainerClassName(tc), name), false, inventory: inv);
            var parties = new Party[] { Game.Instance.Save.PlayerParty, enemyParty };
            Song music = GetTrainerClassSong(tc);
            BattleMaker.CreateTrainerBattle_1v1(enemyInfo, parties, weather, behavior, format, music, tc, defeatText);
        }

        public static void Debug_CreateTestTrainerBattle()
        {
            MapWeather weather = MapWeather.None;
            BlocksetBlockBehavior behavior = BlocksetBlockBehavior.None;
            PBEBattleFormat format = PBEBattleFormat.Rotation;

            TrainerClass tc = TrainerClass.Lady;
            Song music = Song.BattleEvil1;
            string name = "Ur Mom";
            string defeatText = "Bruh";

            (PBEItem, uint)[] inv = null;
            var enemyParty = new Party();
            AddMon(PBESpecies.Wailord, 0, 1, PBEGender.Male, PBENature.Bashful, enemyParty);
            AddMon(PBESpecies.Wailord, 0, 1, PBEGender.Male, PBENature.Bashful, enemyParty);
            AddMon(PBESpecies.Wailord, 0, 1, PBEGender.Male, PBENature.Bashful, enemyParty);
            AddMon(PBESpecies.Togepi, 0, 1, PBEGender.Male, PBENature.Bashful, enemyParty);
            AddMon(PBESpecies.Togepi, 0, 1, PBEGender.Male, PBENature.Bashful, enemyParty);
            AddMon(PBESpecies.Togepi, 0, 1, PBEGender.Male, PBENature.Bashful, enemyParty);

            var enemyInfo = new PBETrainerInfo(enemyParty, string.Format("{0} {1}", GetTrainerClassName(tc), name), false, inventory: inv);
            var parties = new Party[] { Game.Instance.Save.PlayerParty, enemyParty };
            BattleMaker.CreateTrainerBattle_1v1(enemyInfo, parties, weather, behavior, format, music, tc, defeatText);
        }
    }
}
