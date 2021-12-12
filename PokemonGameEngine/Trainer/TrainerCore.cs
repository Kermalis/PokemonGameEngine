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
                case TrainerClass.Leader: return Song.GymBattle;
            }
            return Song.TrainerBattle;
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

        private static Party Debug_CreateParty(bool temp)
        {
            var ret = new Party();
            PartyPokemon p;
            //PartyPokemon p2;
            if (temp)
            {
                p = PartyPokemon.CreateWildMon(PBESpecies.Giratina, PBEForm.Giratina_Origin, 20, PBEGender.Genderless, PBENature.Bashful, BaseStats.Get(PBESpecies.Giratina, PBEForm.Giratina_Origin, true));
                //p2 = PartyPokemon.CreateWildMon(PBESpecies.Shaymin, PBEForm.Shaymin_Sky, 20, PBEGender.Genderless, PBENature.Bashful, BaseStats.Get(PBESpecies.Shaymin, PBEForm.Shaymin_Sky, true));
            }
            else
            {
                p = PartyPokemon.CreateWildMon(PBESpecies.Arceus, PBEForm.Arceus_Dragon, 50, PBEGender.Genderless, PBENature.Bashful, BaseStats.Get(PBESpecies.Arceus, PBEForm.Arceus_Dragon, true));
                //p2 = PartyPokemon.CreateWildMon(PBESpecies.Arceus, PBEForm.Arceus_Dark, 50, PBEGender.Genderless, PBENature.Bashful, BaseStats.Get(PBESpecies.Arceus, PBEForm.Arceus_Dark, true));
            }
            ret.Add(p);
            //ret.Add(p2);
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
            Song song = GetTrainerClassSong(tc);
            BattleMaker.CreateTrainerBattle_1v1(weather, behavior, parties, enemyInfo, format, song, tc, defeatText);
        }
    }
}
