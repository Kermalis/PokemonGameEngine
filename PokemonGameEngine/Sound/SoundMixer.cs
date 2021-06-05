using Kermalis.PokemonGameEngine.Util;
using SDL2;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kermalis.PokemonGameEngine.Sound
{
    internal static class SoundMixer
    {
        private const int SampleRate = 48000;
        public const float SampleRateReciprocal = 1f / SampleRate;

        private static uint _audioDevice;
        private static SDL.SDL_AudioSpec _audioSpec;
        private static float[] _buffer;

        private static SoundChannel _channelList;
        private static DateTime _lastRenderTime;
        public static TimeSpan TimeSinceLastRender;

        public static void Init()
        {
            var spec = new SDL.SDL_AudioSpec();
            spec.freq = SampleRate;
            spec.format = SDL.AUDIO_F32;
            spec.channels = 2;
            spec.samples = 4096;
            spec.callback = MixAudio;
            _audioDevice = SDL.SDL_OpenAudioDevice(null, 0, ref spec, out _audioSpec, 0);
            _buffer = new float[_audioSpec.samples * 2];
            SDL.SDL_PauseAudioDevice(_audioDevice, 0); // Start playing
            _lastRenderTime = DateTime.Now;
        }
        public static void DeInit()
        {
            SDL.SDL_CloseAudioDevice(_audioDevice);
            SDL.SDL_AudioQuit();
        }

        public static void AddChannel(SoundChannel c)
        {
            if (_channelList is null)
            {
                _channelList = c;
            }
            else
            {
                SoundChannel old = _channelList;
                _channelList = c;
                old.Prev = c;
                c.Next = old;
            }
        }
        public static void StopChannel(SoundChannel c)
        {
            if (c == _channelList)
            {
                SoundChannel next = c.Next;
                if (next is not null)
                {
                    next.Prev = null;
                }
                _channelList = next;
            }
            else
            {
                SoundChannel prev = c.Prev;
                SoundChannel next = c.Next;
                if (next is not null)
                {
                    next.Prev = prev;
                }
                prev.Next = next;
            }
            c.Data.DeductReference(); // Dispose wav if it's not being shared
            c.OnStopped?.Invoke(c);
        }

        public static double GetFadeProgress(TimeSpan end, ref TimeSpan cur)
        {
            cur += TimeSinceLastRender;
            if (cur >= end)
            {
                return 1;
            }
            return Utils.GetProgress(end, cur);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFadeVolume(double progress, float from, float to)
        {
            return (float)(from + ((to - from) * progress));
        }
        public static float UpdateFade(float from, float to, TimeSpan end, ref TimeSpan cur)
        {
            cur += TimeSinceLastRender;
            if (cur >= end)
            {
                return to;
            }
            double p = Utils.GetProgress(end, cur);
            return GetFadeVolume(p, from, to);
        }

#if DEBUG
        // Draws bars in the console
        // TODO: How do we actually draw f32 audio?
        public static void Debug_DrawAudio()
        {
            float peakL = -1f;
            float peakR = -1f;
            for (int i = 0; i < _buffer.Length / 2; i++)
            {
                float l = _buffer[i * 2];
                float r = _buffer[i * 2 + 1];
                if (l > peakL)
                {
                    peakL = l;
                }
                if (r > peakR)
                {
                    peakR = r;
                }
            }

            string str;
            void Str(float peak)
            {
                const int numBars = 200;
                str = string.Empty;
                float num;
                if (peak <= -1)
                {
                    num = 0;
                }
                else if (peak >= 1)
                {
                    num = numBars;
                }
                else
                {
                    num = (peak + 1) / 2 * numBars;
                }
                for (int i = 0; i < num; i++)
                {
                    str += '|';
                }
                str = str.PadRight(numBars);
            }
            Str(peakL);
            Console.WriteLine("L: {0:0.000}\t[{1}]", peakL, str);
            Str(peakR);
            Console.WriteLine("R: {0:0.000}\t[{1}]\n", peakR, str);
        }
#endif

        // F32 audio can legally be above 1f and below -1f
        // That's also true for audio sources, since you can lower the volume and still result with the "original" audio
        // So there's no clipping applied and all mixing is applied with +, while volume is applied with *

        private static void MixAudio(IntPtr userdata, IntPtr stream, int len)
        {
            DateTime renderTime = DateTime.Now;
            if (renderTime <= _lastRenderTime)
            {
                TimeSinceLastRender = new TimeSpan(0, 0, 0, 0, SampleRate / 1_000); // Probably wrong
            }
            else
            {
                TimeSinceLastRender = renderTime.Subtract(_lastRenderTime);
            }
            SoundControl.SoundLogicTick(); // Run sound tasks

            int numSamples = len / (2 * sizeof(float)); // 2 Channels
            Array.Clear(_buffer, 0, numSamples * 2);

            for (SoundChannel c = _channelList; c is not null; c = c.Next)
            {
                if (!c.IsPaused)
                {
                    c.MixF32(_buffer, numSamples);
                }
            }

#if DEBUG
            Debug_DrawAudio();
#endif

            // Marshal copy is at least twice as fast as sdl memset
            Marshal.Copy(_buffer, 0, stream, numSamples * 2);

            _lastRenderTime = renderTime;
        }

        #region Mixing Math

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float U8ToF32(int sample, float vol)
        {
            return (sample - 128) / 128f * vol;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float S16ToF32(int sample, float vol)
        {
            return sample / 32768f * vol;
        }

        #endregion
    }
}
