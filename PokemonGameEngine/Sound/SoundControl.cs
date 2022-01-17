using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.World;
using System;

namespace Kermalis.PokemonGameEngine.Sound
{
    internal static partial class SoundControl
    {
        public static readonly object LockObj = new();
        public static readonly ConnectedList<BackTask> Tasks = new(BackTask.Sorter);

        public static int GetCryPitch(float hpPercentage)
        {
            return (int)((1 - hpPercentage) * -96); // 1/8 of -768; so -0.125 semitones for a fainted mon
        }

        public static SoundChannel PlayCryFromHP(PBESpecies species, PBEForm form, float hpPercentage, float vol = 0.5f, float pan = 0f)
        {
            return PlayCry(species, form, vol: vol, pan: pan, pitch: GetCryPitch(hpPercentage));
        }
        public static SoundChannel PlayCry(PBESpecies species, PBEForm form, float vol = 0.5f, float pan = 0f, int pitch = 0)
        {
            var channel = new SoundChannel(GetCryAsset(species, form)) { Volume = vol, Panpot = pan };
            channel.SetPitch(pitch);
            SoundMixer.AddChannel(channel);
            return channel;
        }

        private static string GetCryAsset(PBESpecies species, PBEForm form)
        {
            string dir = @"Sound\Cries\";
            if (species == PBESpecies.Shaymin && form == PBEForm.Shaymin_Sky)
            {
                return dir + "Shaymin_Sky.wav";
            }
            if (species == PBESpecies.Tornadus && form == PBEForm.Tornadus_Therian)
            {
                return dir + "Tornadus_Therian.wav";
            }
            if (species == PBESpecies.Thundurus && form == PBEForm.Thundurus_Therian)
            {
                return dir + "Thundurus_Therian.wav";
            }
            if (species == PBESpecies.Landorus && form == PBEForm.Landorus_Therian)
            {
                return dir + "Landorus_Therian.wav";
            }
            if (species == PBESpecies.Kyurem)
            {
                if (form == PBEForm.Kyurem_White)
                {
                    return dir + "Kyurem_White.wav";
                }
                if (form == PBEForm.Kyurem_Black)
                {
                    return dir + "Kyurem_Black.wav";
                }
            }
            return dir + species + ".wav";
        }
        public static string GetSongAsset(Song song)
        {
            string dir = @"Sound\BGM\";
            switch (song)
            {
                // Locations
                case Song.Route1: // TODO: Route theme
                case Song.Town1:
                    return dir + "Town1.wav";
                case Song.Cave1:
                    return dir + "Cave1.wav";
                // Battles
                case Song.BattleWild: // TODO: Wild battle themes
                case Song.BattleWild_Multi:
                case Song.BattleTrainer:
                    return dir + "BattleTrainer.wav";
                case Song.BattleLegendary: // TODO: Legendary battle theme
                case Song.BattleGymLeader:
                    return dir + "BattleGymLeader.wav";
                case Song.BattleEvil1:
                    return dir + "BattleEvil1.wav";
            }
            throw new ArgumentOutOfRangeException(nameof(song));
        }

        // Called every time SoundMixer mixes
        public static void RunSoundTasks()
        {
            for (BackTask t = Tasks.First; t is not null; t = t.Next)
            {
                t.Action(t);
            }
        }
    }
}
