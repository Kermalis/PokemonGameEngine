using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Util;
using Kermalis.PokemonGameEngine.World;
using SoLoud;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Sound
{
    internal static class SoundControl
    {
        private sealed class Sound
        {
            public readonly Song Song;
            public readonly WavStream Wav;
            public uint Handle;

            public Sound(Song song, WavStream wav)
            {
                Song = song;
                Wav = wav;
            }
        }
        private sealed class CrySound
        {
            public readonly Wav Wav;
            public uint Handle;

            public CrySound(Wav wav)
            {
                Wav = wav;
            }
        }

        private static readonly Dictionary<Song, (string resource, double loopPoint)> _songResources = new Dictionary<Song, (string, double)>
        {
            { Song.Town1, ("Sound.BGM.Town1.ogg", 60d/110*4*14) }, // 110BPM, 4/4, loop after 14 bars (30.5454~ seconds)
            { Song.Route1, ("Sound.BGM.Town1.ogg", 60d/110*4*14) },//, ("Sound.BGM.Route1.ogg", 60d/220*4*2) }, // 220BPM, 4/4, loop after 2 bars (2.1818~ seconds)
            { Song.Cave1, ("Sound.BGM.Cave1.ogg", 60d/128*4*2) }, // 128BPM, 4/4, loop after 2 bars (3.75 seconds)
            { Song.WildBattle, ("Sound.BGM.TrainerBattle.ogg", 60d/270*4*40) },
            { Song.TrainerBattle, ("Sound.BGM.TrainerBattle.ogg", 60d/270*4*40) }, // 270BPM, 4/4, loop after 40 bars (35.55~ seconds)
            { Song.GymBattle, ("Sound.BGM.TrainerBattle.ogg", 60d/270*4*40) },
            { Song.LegendaryBattle, ("Sound.BGM.TrainerBattle.ogg", 60d/270*4*40) },
        };

        private static readonly Soloud _soloud;
        private static Sound _overworldBGM;
        private static Song _newOverworldBGM;
        private static Sound _battleBGM;
        private static CrySound _testCry;

        static SoundControl()
        {
            _soloud = new Soloud();
        }
        public static void Init()
        {
            _soloud.init(Soloud.CLIP_ROUNDOFF, Soloud.SDL2);
        }
        public static void DeInit()
        {
            _soloud.deinit();
        }

        // Ideally we would want to be using wav.loadFile(), but I'm not sure if it's possible from C#
        private static unsafe Sound SongToSound(Song song)
        {
            byte[] bytes;
            (string resource, double loopPoint) = _songResources[song];
            using (Stream stream = Utils.GetResourceStream(resource))
            {
                bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
            }
            var wav = new WavStream();
            fixed (byte* b = bytes)
            {
                wav.loadMem(new IntPtr(b), (uint)bytes.Length, aCopy: 1);
            }
            if (loopPoint != 0)
            {
                wav.setLoopPoint(loopPoint);
            }
            wav.setLooping(1);
            return new Sound(song, wav);
        }
        private static unsafe CrySound CryToSound(PBESpecies species, PBEForm form)
        {
            byte[] bytes;
            using (Stream stream = Utils.GetResourceStream(GetCryResource(species, form)))
            {
                bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
            }
            var wav = new Wav();
            fixed (byte* b = bytes)
            {
                wav.loadMem(new IntPtr(b), (uint)bytes.Length, aCopy: 1);
            }
            return new CrySound(wav);
        }

        private static string GetCryResource(PBESpecies species, PBEForm form)
        {
            if (species == PBESpecies.Shaymin && form == PBEForm.Shaymin_Sky)
            {
                return "Sound.Cries.Shaymin_Sky.wav";
            }
            return "Sound.Cries." + species + ".wav";
        }

        public static void Debug_PlayCry(PBESpecies species, PBEForm form)
        {
            _testCry = CryToSound(species, form);
            _testCry.Handle = _soloud.play(_testCry.Wav);
        }

        public static void SetOverworldBGM_NoFade(Song song)
        {
            if (_overworldBGM != null)
            {
                _soloud.stop(_overworldBGM.Handle);
            }
            _overworldBGM = SongToSound(song);
            _overworldBGM.Handle = _soloud.playBackground(_overworldBGM.Wav);
        }
        public static void SetOverworldBGM(Song song)
        {
            if (_overworldBGM is null)
            {
                _overworldBGM = SongToSound(song);
                _overworldBGM.Handle = _soloud.playBackground(_overworldBGM.Wav);
                return;
            }
            // No need to do anything if it's the same song
            if (_overworldBGM.Song == song)
            {
                return;
            }
            // Fade to nothing
            if (song == Song.None)
            {
                _soloud.fadeVolume(_overworldBGM.Handle, 0, 1);
                _soloud.scheduleStop(_overworldBGM.Handle, 1);
                Game.Instance.SetSCallback(SCB_FadingOutOverworldToNothing);
                return;
            }
            // Fade to something
            _newOverworldBGM = song;
            _soloud.fadeVolume(_overworldBGM.Handle, 0, 1);
            _soloud.scheduleStop(_overworldBGM.Handle, 1);
            Game.Instance.SetSCallback(SCB_FadingOutOverworldToOverworld);
        }

        public static void FadeOutBattleBGMToOverworldBGM()
        {
            _soloud.fadeVolume(_battleBGM.Handle, 0, 1);
            _soloud.scheduleStop(_battleBGM.Handle, 1);
            Game.Instance.SetSCallback(SCB_FadingOutBattleToOverworld);
        }
        public static void SetBattleBGM(Song song)
        {
            // Assuming you're not setting battle bgm twice in a row
            if (_overworldBGM != null)
            {
                _soloud.setPause(_overworldBGM.Handle, 1);
                _soloud.setVolume(_overworldBGM.Handle, 0);
            }
            _battleBGM = SongToSound(song);
            _battleBGM.Handle = _soloud.playBackground(_battleBGM.Wav);
        }

        private static void SCB_FadingOutBattleToOverworld()
        {
            if (_soloud.getVolume(_battleBGM.Handle) <= 0)
            {
                _soloud.stop(_battleBGM.Handle);
                _battleBGM = null;
                if (_overworldBGM != null)
                {
                    _soloud.setPause(_overworldBGM.Handle, 0);
                    _soloud.fadeVolume(_overworldBGM.Handle, 1, 1);
                }
                Game.Instance.SetSCallback(null);
            }
        }
        private static void SCB_FadingOutOverworldToOverworld()
        {
            if (_soloud.getVolume(_overworldBGM.Handle) <= 0)
            {
                _soloud.stop(_overworldBGM.Handle);
                _overworldBGM = SongToSound(_newOverworldBGM);
                _newOverworldBGM = Song.None;
                _overworldBGM.Handle = _soloud.playBackground(_overworldBGM.Wav);
                Game.Instance.SetSCallback(null);
            }
        }
        private static void SCB_FadingOutOverworldToNothing()
        {
            if (_soloud.getVolume(_overworldBGM.Handle) <= 0)
            {
                _soloud.stop(_overworldBGM.Handle);
                _overworldBGM = null;
                Game.Instance.SetSCallback(null);
            }
        }
    }
}
