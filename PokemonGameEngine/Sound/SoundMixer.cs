using Kermalis.PokemonGameEngine.Core;
using SDL2;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kermalis.PokemonGameEngine.Sound
{
    internal static class SoundMixer
    {
        private const int SAMPLE_RATE = 48_000;
        public const float SAMPLE_RATE_RECIPROCAL = 1f / SAMPLE_RATE;

        private static readonly uint _audioDevice;
        private static readonly SDL.SDL_AudioSpec _audioSpec;
        private static readonly float[] _buffer;
        private static readonly float[] _tempBuffer;

        private static readonly ConnectedList<SoundChannel> _channels = new(SoundChannel.Sorter);
        private static DateTime _lastRenderTime;
        public static float DeltaTime;

        static SoundMixer()
        {
            var spec = new SDL.SDL_AudioSpec();
            spec.freq = SAMPLE_RATE;
            spec.format = SDL.AUDIO_F32;
            spec.channels = 2;
            spec.samples = 4096;
            spec.callback = MixAudio;

            _audioDevice = SDL.SDL_OpenAudioDevice(null, 0, ref spec, out _audioSpec, 0);
            int len = _audioSpec.samples * 2;
            _buffer = new float[len];
            _tempBuffer = new float[len];
            SDL.SDL_PauseAudioDevice(_audioDevice, 0); // Start playing
            _lastRenderTime = DateTime.Now;
        }
        public static void Quit()
        {
            SDL.SDL_CloseAudioDevice(_audioDevice);
            SDL.SDL_AudioQuit();
        }

        public static void AddChannel(SoundChannel c)
        {
            _channels.Add(c);
        }
        public static void StopChannel(SoundChannel c)
        {
            _channels.Remove(c);
        }

#if DEBUG_AUDIO_LOG
        // Draws bars in the console
        public static void Debug_DrawAudio()
        {
            static float AdjustPeak(float inPeak, float value)
            {
                if (inPeak == 0)
                {
                    return value;
                }
                if (value == 0)
                {
                    return inPeak;
                }
                // Positive value
                if (value > 0)
                {
                    if (inPeak > 0)
                    {
                        return value > inPeak ? value : inPeak; // Positive peak
                    }
                    return -value < inPeak ? value : inPeak; // Negative peak
                }
                // Negative value
                if (inPeak > 0)
                {
                    return -value > inPeak ? value : inPeak; // Positive peak
                }
                return value < inPeak ? value : inPeak; // Negative peak
            }

            float peakL = 0f;
            float peakR = 0f;
            for (int i = 0; i < _buffer.Length / 2; i++)
            {
                peakL = AdjustPeak(peakL, _buffer[i * 2]);
                peakR = AdjustPeak(peakR, _buffer[i * 2 + 1]);
            }

            string str;
            void Str(float peak)
            {
                const int numBars = 200;
                str = string.Empty;
                float absPeak = Math.Abs(peak);
                float num;
                if (absPeak >= 1)
                {
                    num = numBars;
                }
                else
                {
                    num = absPeak * numBars;
                }
                for (int i = 0; i < num; i++)
                {
                    str += '|';
                }
                str = str.PadRight(numBars);
            }
            Str(peakL);
            Console.WriteLine("L: {0}\t[{1}]", peakL.ToString("0.000").PadLeft(6), str);
            Str(peakR);
            Console.WriteLine("R: {0}\t[{1}]\n", peakR.ToString("0.000").PadLeft(6), str);
        }
#endif

        // F32 audio can legally be above 1f and below -1f
        // That's also true for audio sources, since you can lower the volume and still result with the "original" audio
        // So there's no clipping applied and all mixing is applied with +, while volume is applied with *

        private static void MixAudio(IntPtr userdata, IntPtr stream, int len)
        {
            lock (SoundControl.LockObj)
            {
                // Calculate delta time
                DateTime now = DateTime.Now;
                if (now <= _lastRenderTime)
                {
                    DeltaTime = 1f; // Assume 1 second passed
                }
                else
                {
                    DeltaTime = (float)(now - _lastRenderTime).TotalSeconds;
                    if (DeltaTime > 1f)
                    {
                        DeltaTime = 1f;
                    }
                }

                SoundControl.RunSoundTasks(); // Run sound tasks

                // Mix audio
                int numSamplesTotal = len / sizeof(float);
                int numSamplesPerChannel = numSamplesTotal / 2; // 2 Channels
                Array.Clear(_buffer, 0, numSamplesTotal);

                for (SoundChannel c = _channels.First; c is not null; c = c.Next)
                {
                    if (c.IsFading)
                    {
                        Array.Clear(_tempBuffer, 0, numSamplesTotal);
                        c.MixF32(_tempBuffer, numSamplesPerChannel);
                        c.ApplyFade(_tempBuffer, numSamplesPerChannel);
                        for (int i = 0; i < numSamplesTotal; i++)
                        {
                            _buffer[i] += _tempBuffer[i];
                        }
                    }
                    else
                    {
                        c.MixF32(_buffer, numSamplesPerChannel);
                    }
                }

#if DEBUG_AUDIO_LOG
                Debug_DrawAudio();
#endif

                // Marshal copy is at least twice as fast as sdl memset
                Marshal.Copy(_buffer, 0, stream, numSamplesTotal);

                _lastRenderTime = now;
            }
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
