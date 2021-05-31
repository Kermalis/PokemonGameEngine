using Kermalis.PokemonGameEngine.Util;
using SDL2;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Kermalis.PokemonGameEngine.Sound
{
    internal static class SoundMixer
    {
        private const int SampleRate = 48000;
        public const float SampleRateReciprocal = 1f / SampleRate;

        private static uint _audioDevice;
        private static SDL.SDL_AudioSpec _audioSpec;
        private static SoundChannel _test;
        private static short[] _buffer;

        public static void Init()
        {
            var spec = new SDL.SDL_AudioSpec();
            spec.freq = SampleRate;
            spec.format = SDL.AUDIO_S16;
            spec.channels = 2;
            spec.samples = 4096;
            spec.callback = Test;
            _test = new SoundChannel(new WaveFileData(Utils.GetResourceStream("Sound.BGM.GymBattle.wav")));
            _audioDevice = SDL.SDL_OpenAudioDevice(null, 0, ref spec, out _audioSpec, 0);
            _buffer = new short[_audioSpec.samples * 2];
            SDL.SDL_PauseAudioDevice(_audioDevice, 0); // Start playing
        }
        public static void DeInit()
        {
            SDL.SDL_CloseAudioDevice(_audioDevice);
        }

        public static unsafe void Test(IntPtr userdata, IntPtr stream, int len)
        {
            int numSamples = len / (2 * sizeof(short)); // 2 Channels
            Array.Clear(_buffer, 0, numSamples * 2);
            _test.MixS16(_buffer, numSamples);

            // Marshal copy is at least twice as fast as sdl memset
            Marshal.Copy(_buffer, 0, stream, numSamples * 2);
        }
    }
}
