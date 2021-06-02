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
            public SoundChannel Handle;

            public Sound(Song song, WaveFileData wav)
            {
                Song = song;
                Wav = wav;
            }

            public void Dispose()
            {
                Wav.Stream.Dispose();
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

            public void Dispose()
            {
                Wav.Stream.Dispose();
            }
        }

        private static readonly TaskList _tasks = new();

        private static Sound _overworldBGM;
        private static Sound _battleBGM;
        private static CrySound _testCry; // TODO: Cry system + actually dispose cries

        public static void DeInit()
        {
            _overworldBGM?.Dispose();
            _battleBGM?.Dispose();
            _testCry?.Dispose();
        }

        private static unsafe CrySound CryToSound(PBESpecies species, PBEForm form)
        {
            var wav = new WaveFileData(Utils.GetResourceStream(GetCryResource(species, form)));
            return new CrySound(wav);
        }
        private static unsafe Sound SongToSound(Song song)
        {
            var wav = new WaveFileData(Utils.GetResourceStream(GetSongResource(song)));
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

        public static void Debug_PlayCry(PBESpecies species, PBEForm form)
        {
            _testCry = CryToSound(species, form);
            _testCry.Handle = new SoundChannel(_testCry.Wav) { Volume = 0.75f }; // TODO: (#62) Volume causes popping if the volume drags up/down the mix
            SoundMixer.StartSound(_testCry.Handle);
        }

        public static void SetOverworldBGM_NoFade(Song song)
        {
            if (_overworldBGM is not null)
            {
                SoundMixer.StopSound(_overworldBGM.Handle);
            }
            _overworldBGM = SongToSound(song);
            _overworldBGM.Handle = new SoundChannel(_overworldBGM.Wav);
            SoundMixer.StartSound(_overworldBGM.Handle);
        }
        public static void SetOverworldBGM(Song song)
        {
            if (_overworldBGM is null)
            {
                _overworldBGM = SongToSound(song);
                _overworldBGM.Handle = new SoundChannel(_overworldBGM.Wav);
                SoundMixer.StartSound(_overworldBGM.Handle);
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
                _overworldBGM.Handle.IsPaused = true;
            }
            _battleBGM = SongToSound(song);
            _battleBGM.Handle = new SoundChannel(_battleBGM.Wav);
            SoundMixer.StartSound(_battleBGM.Handle);
        }

        // Called every time SoundMixer mixes
        public static void SoundLogicTick()
        {
            _tasks.RunTasks();
        }
    }
}
