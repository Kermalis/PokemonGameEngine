using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.World;
using System;

namespace Kermalis.PokemonGameEngine.Sound
{
    internal static partial class SoundControl
    {
        private sealed class Sound
        {
            public readonly Song Song;
            public readonly WaveFileData Data;
            public SoundChannel Channel;

            public Sound(Song song, WaveFileData data)
            {
                Song = song;
                Data = data;
            }
        }
        private sealed class CrySound
        {
            public readonly WaveFileData Data;
            public SoundChannel Channel;

            public CrySound(WaveFileData data)
            {
                Data = data;
            }
        }

        private static readonly ConnectedList<BackTask> _tasks = new(BackTask.Sorter);

        private static Sound _overworldBGM;
        private static Sound _battleBGM;
        private static CrySound _testCry; // TODO: Cry system

        private static CrySound LoadCrySound(PBESpecies species, PBEForm form)
        {
            var data = WaveFileData.Get(GetCryAsset(species, form));
            return new CrySound(data);
        }
        private static Sound LoadSongSound(Song song)
        {
            var data = WaveFileData.Get(GetSongAsset(song));
            return new Sound(song, data);
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
        private static string GetSongAsset(Song song)
        {
            string dir = @"Sound\BGM\";
            switch (song)
            {
                // Locations
                case Song.Route1: // TODO
                case Song.Town1: return dir + "Town1.wav";
                case Song.Cave1: return dir + "Cave1.wav";
                // Battles
                case Song.WildBattle: // TODO
                case Song.WildBattle_Multi: // TODO
                case Song.TrainerBattle: return dir + "TrainerBattle.wav";
                case Song.LegendaryBattle: // TODO
                case Song.GymBattle: return dir + "GymBattle.wav";
            }
            throw new ArgumentOutOfRangeException(nameof(song));
        }

        public static int GetCryPitch(float hpPercentage)
        {
            return (int)((1 - hpPercentage) * -96); // 1/8 of -768; so -0.125 semitones for a fainted mon
        }

        public static void PlayCryFromHP(PBESpecies species, PBEForm form, float hpPercentage, float vol = 0.5f, float pan = 0f, SoundStoppedFunc onStopped = null)
        {
            PlayCry(species, form, vol: vol, pan: pan, pitch: GetCryPitch(hpPercentage), onStopped: onStopped);
        }
        public static void PlayCry(PBESpecies species, PBEForm form, float vol = 0.5f, float pan = 0f, int pitch = 0, SoundStoppedFunc onStopped = null)
        {
            _testCry = LoadCrySound(species, form);
            _testCry.Channel = new SoundChannel(_testCry.Data) { Volume = vol, Panpot = pan, OnStopped = onStopped };
            _testCry.Channel.SetPitch(pitch);
            SoundMixer.AddChannel(_testCry.Channel);
        }

        public static void SetOverworldBGM_NoFade(Song song)
        {
            if (_overworldBGM is not null)
            {
                SoundMixer.StopChannel(_overworldBGM.Channel);
            }
            _overworldBGM = LoadSongSound(song);
            _overworldBGM.Channel = new SoundChannel(_overworldBGM.Data);
            SoundMixer.AddChannel(_overworldBGM.Channel);
        }
        public static void SetOverworldBGM(Song song)
        {
            if (_overworldBGM is null)
            {
                _overworldBGM = LoadSongSound(song);
                _overworldBGM.Channel = new SoundChannel(_overworldBGM.Data);
                SoundMixer.AddChannel(_overworldBGM.Channel);
                return;
            }
            // Fade if the song is different
            CreateOrUpdateFadeOverworldBGMAndStartNewSongTask(song, 1f, 1f, 0f);
        }

        public static void FadeOutBattleBGMToOverworldBGM()
        {
            CreateFadeBattleBGMToOverworldBGMTask();
        }
        public static void SetBattleBGM(Song song)
        {
            // Assuming you're not setting battle bgm twice in a row
            if (_overworldBGM is not null)
            {
                _overworldBGM.Channel.IsPaused = true;
            }
            _battleBGM = LoadSongSound(song);
            _battleBGM.Channel = new SoundChannel(_battleBGM.Data);
            SoundMixer.AddChannel(_battleBGM.Channel);
        }

        // Called every time SoundMixer mixes
        public static void RunSoundTasks()
        {
            for (BackTask t = _tasks.First; t is not null; t = t.Next)
            {
                t.Action(t);
            }
        }
    }
}
