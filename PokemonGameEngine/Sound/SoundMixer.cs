using Kermalis.EndianBinaryIO;
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
        private static short[] _buffer;

        private static SoundChannel ChannelList;
        private static DateTime _lastRenderTime;
        public static TimeSpan TimeSinceLastRender;

        public static void Init()
        {
            var spec = new SDL.SDL_AudioSpec();
            spec.freq = SampleRate;
            spec.format = SDL.AUDIO_S16;
            spec.channels = 2;
            spec.samples = 4096;
            spec.callback = MixAudio;
            _audioDevice = SDL.SDL_OpenAudioDevice(null, 0, ref spec, out _audioSpec, 0);
            _buffer = new short[_audioSpec.samples * 2];
            SDL.SDL_PauseAudioDevice(_audioDevice, 0); // Start playing
            _lastRenderTime = DateTime.Now;
        }
        public static void DeInit()
        {
            SoundControl.DeInit();
            SDL.SDL_CloseAudioDevice(_audioDevice);
            SDL.SDL_AudioQuit();
        }

        public static SoundChannel StartSound(WaveFileData data)
        {
            var c = new SoundChannel(data);
            if (ChannelList is null)
            {
                ChannelList = c;
            }
            else
            {
                SoundChannel old = ChannelList;
                ChannelList = c;
                old.Prev = c;
                c.Next = old;
            }
            return c;
        }
        public static void StopSound(SoundChannel c)
        {
            if (c == ChannelList)
            {
                SoundChannel next = c.Next;
                if (next is not null)
                {
                    next.Prev = null;
                }
                ChannelList = next;
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

            int numSamples = len / (2 * sizeof(short)); // 2 Channels
            Array.Clear(_buffer, 0, numSamples * 2);

            for (SoundChannel c = ChannelList; c is not null; c = c.Next)
            {
                if (!c.IsPaused)
                {
                    c.MixS16(_buffer, numSamples);
                }
            }

            // Marshal copy is at least twice as fast as sdl memset
            Marshal.Copy(_buffer, 0, stream, numSamples * 2);

            _lastRenderTime = renderTime;
        }

        #region Mixing Math

        // https://stackoverflow.com/a/25102339
        // This can be adapted for s8 as well (and for unsigned if the += and -= are removed)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MixU16Samples(int a, int b)
        {
            int m;
            if ((a <= short.MaxValue) || (b <= short.MaxValue))
            {
                m = (a * b) >> 15; // ">> 15" is the same as "/ (short.MaxValue+1)"
            }
            else // Two large values
            {
                m = (2 * (a + b)) - ((a * b) >> 15) - (ushort.MaxValue + 1);
            }

            if (m > ushort.MaxValue)
            {
                m = ushort.MaxValue;
            }
            return m;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MixS16Samples(short[] buffer, int index, short sample, float vol)
        {
            const int magic = short.MaxValue + 1;
            int a = buffer[index] + magic; // Convert a to u16
            int b = (int)(sample * vol) + magic; // Convert b to u16
            int m = MixU16Samples(a, b);
            m -= magic; // Convert back to s16
            buffer[index] = (short)m;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MixU8AndS16Sample(short[] buffer, int index, byte sample, float vol)
        {
            const int magic = short.MaxValue + 1;
            int a = buffer[index] + magic; // Convert a to u16
            int b = (int)(sample * 257 * vol); // Convert b to u16
            int m = MixU16Samples(a, b);
            m -= magic; // Convert back to s16
            buffer[index] = (short)m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MixU8Samples_Mono(short[] buffer, int index, EndianBinaryReader r, long offset, float leftVol, float rightVol)
        {
            r.BaseStream.Position = offset;
            byte samp = r.ReadByte();
            MixU8AndS16Sample(buffer, index, samp, leftVol);
            MixU8AndS16Sample(buffer, index + 1, samp, rightVol);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MixU8Samples_Stereo(short[] buffer, int index, EndianBinaryReader r, long offset, float leftVol, float rightVol)
        {
            r.BaseStream.Position = offset;
            MixU8AndS16Sample(buffer, index, r.ReadByte(), leftVol);
            MixU8AndS16Sample(buffer, index + 1, r.ReadByte(), rightVol);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MixS16Samples_Mono(short[] buffer, int index, EndianBinaryReader r, long offset, float leftVol, float rightVol)
        {
            r.BaseStream.Position = offset;
            short samp = r.ReadInt16();
            MixS16Samples(buffer, index, samp, leftVol);
            MixS16Samples(buffer, index + 1, samp, rightVol);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MixS16Samples_Stereo(short[] buffer, int index, EndianBinaryReader r, long offset, float leftVol, float rightVol)
        {
            r.BaseStream.Position = offset;
            MixS16Samples(buffer, index, r.ReadInt16(), leftVol);
            MixS16Samples(buffer, index + 1, r.ReadInt16(), rightVol);
        }

        #endregion
    }
}
