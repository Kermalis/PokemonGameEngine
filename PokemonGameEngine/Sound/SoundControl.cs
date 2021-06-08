using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Util;
using Kermalis.PokemonGameEngine.World;
using System;

namespace Kermalis.PokemonGameEngine.Sound
{
    internal static partial class SoundControl
    {
        private sealed class Sound
        {
            public readonly Song Song;
            public readonly WaveFileData Wav;
            public SoundChannel Channel;

            public Sound(Song song, WaveFileData wav)
            {
                Song = song;
                Wav = wav;
            }
        }
        private sealed class CrySound
        {
            public readonly WaveFileData Wav;
            public SoundChannel Handle;

            public CrySound(WaveFileData wav)
            {
                Wav = wav;
            }
        }

        private static readonly TaskList _tasks = new();

        private static Sound _overworldBGM;
        private static Sound _battleBGM;
        private static CrySound _testCry; // TODO: Cry system

        private static unsafe CrySound CryToSound(PBESpecies species, PBEForm form)
        {
            var wav = WaveFileData.Get(GetCryResource(species, form));
            return new CrySound(wav);
        }
        private static unsafe Sound SongToSound(Song song)
        {
            var wav = WaveFileData.Get(GetSongResource(song));
            return new Sound(song, wav);
        }

        private static string GetCryResource(PBESpecies species, PBEForm form)
        {
            if (species == PBESpecies.Shaymin && form == PBEForm.Shaymin_Sky)
            {
                return "Sound.Cries.Shaymin_Sky.wav";
            }
            return "Sound.Cries." + species + ".wav";
        }
        private static string GetSongResource(Song song)
        {
            switch (song)
            {
                // Locations
                case Song.Route1: // TODO
                case Song.Town1: return "Sound.BGM.Town1.wav";
                case Song.Cave1: return "Sound.BGM.Cave1.wav";
                // Battles
                case Song.WildBattle: // TODO
                case Song.TrainerBattle: return "Sound.BGM.TrainerBattle.wav";
                case Song.LegendaryBattle: // TODO
                case Song.GymBattle: return "Sound.BGM.GymBattle.wav";
            }
            throw new ArgumentOutOfRangeException(nameof(song));
        }

        public static int GetCryPitch(double hpPercentage)
        {
            return (int)((1 - hpPercentage) * -192); // 1/4 of -768; so -0.25 semitones for a fainted mon
        }

        public static void PlayCry(PBESpecies species, PBEForm form, double hpPercentage, float vol = 0.5f, float pan = 0f, SoundStoppedFunc onStopped = null)
        {
            PlayCry(species, form, vol: vol, pan: pan, pitch: GetCryPitch(hpPercentage), onStopped: onStopped);
        }
        public static void PlayCry(PBESpecies species, PBEForm form, float vol = 0.5f, float pan = 0f, int pitch = 0, SoundStoppedFunc onStopped = null)
        {
            _testCry = CryToSound(species, form);
            _testCry.Handle = new SoundChannel(_testCry.Wav) { Volume = vol, Panpot = pan, OnStopped = onStopped };
            _testCry.Handle.SetPitch(pitch);
            SoundMixer.AddChannel(_testCry.Handle);
        }

        public static void SetOverworldBGM_NoFade(Song song)
        {
            if (_overworldBGM is not null)
            {
                SoundMixer.StopChannel(_overworldBGM.Channel);
            }
            _overworldBGM = SongToSound(song);
            _overworldBGM.Channel = new SoundChannel(_overworldBGM.Wav);
            SoundMixer.AddChannel(_overworldBGM.Channel);
        }
        public static void SetOverworldBGM(Song song)
        {
            if (_overworldBGM is null)
            {
                _overworldBGM = SongToSound(song);
                _overworldBGM.Channel = new SoundChannel(_overworldBGM.Wav);
                SoundMixer.AddChannel(_overworldBGM.Channel);
                return;
            }
            // Fade if the song is different
            CreateOrUpdateFadeOverworldBGMAndStartNewSongTask(song, 1_000, 1f, 0f);
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
            _battleBGM = SongToSound(song);
            _battleBGM.Channel = new SoundChannel(_battleBGM.Wav);
            SoundMixer.AddChannel(_battleBGM.Channel);
        }

        // Called every time SoundMixer mixes
        public static void SoundLogicTick()
        {
            _tasks.RunTasks();
        }
    }
}
