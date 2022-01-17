using Kermalis.PokemonGameEngine.Core;
using System;
using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Sound
{
    internal sealed class SoundChannel : IConnectedListObject<SoundChannel>
    {
        public SoundChannel Next { get; set; }
        public SoundChannel Prev { get; set; }

        public bool IsStopped;
        /// <summary>Higher priorities are mixed first</summary>
        public int Priority;
        public float Volume;
        /// <summary>-1 left, 0 center, +1 right</summary>
        public float Panpot;

        public readonly WaveFileData Data;

        // Playback
        private float _freq;
        private float _interPos;
        private long _offset;
        private long _trailOffset;

        // Fade
        public bool IsFading;
        private float _fadeFrom;
        private float _fadeTo;
        private float _fadeCurVolume;
        private float _fadeTime;
        private float _fadeDuration;

        public SoundChannel(string asset)
        {
            Volume = 1f;
            Panpot = 0f;
            Data = WaveFileData.Get(asset);
            _freq = Data.SampleRate;
            _offset = Data.DataStart;
            _trailOffset = Data.DataEnd;
        }
        public SoundChannel(SoundChannelState state)
        {
            Priority = state.Priority;
            Volume = 1f;
            Panpot = state.Panpot;
            Data = WaveFileData.Get(state.Asset);
            _freq = state.Freq;
            _interPos = state.InterPos;
            _offset = state.Offset;
            _trailOffset = state.TrailOffset;
        }

        public static int Sorter(SoundChannel s1, SoundChannel s2)
        {
            if (s1.Priority > s2.Priority)
            {
                return -1;
            }
            if (s1.Priority == s2.Priority)
            {
                return 0;
            }
            return 1;
        }

        public SoundChannelState GetState()
        {
            return new SoundChannelState(Data.Asset, Panpot, Priority, _freq, _interPos, _offset, _trailOffset);
        }

        public void SetPitch(int pitch)
        {
            // MathF.Pow(2, ((Key - 60) / 12f) + (pitch / 768f))
            // If we had a key we'd use the above. Instead we're emulating base key
            _freq = Data.SampleRate * MathF.Pow(2, pitch / 768f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetLeftVol()
        {
            float lAmp = 1 - (Panpot / 2 + 0.5f);
            return Volume * lAmp;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetRightVol()
        {
            float rAmp = Panpot / 2 + 0.5f;
            return Volume * rAmp;
        }

        public void MixF32(float[] buffer, int numSamples)
        {
            float leftVol = GetLeftVol();
            float rightVol = GetRightVol();
            if (Data.DoesLoop)
            {
                if (Data.Channels == 1)
                {
                    switch (Data.BitsPerSample)
                    {
                        case 8: MixU8_Mono_Loop(buffer, numSamples, leftVol, rightVol); break;
                        case 16: MixS16_Mono_Loop(buffer, numSamples, leftVol, rightVol); break;
                        default: MixF32_Mono_Loop(buffer, numSamples, leftVol, rightVol); break;
                    }
                }
                else
                {
                    switch (Data.BitsPerSample)
                    {
                        case 8: MixU8_Stereo_Loop(buffer, numSamples, leftVol, rightVol); break;
                        case 16: MixS16_Stereo_Loop(buffer, numSamples, leftVol, rightVol); break;
                        default: MixF32_Stereo_Loop(buffer, numSamples, leftVol, rightVol); break;
                    }
                }
            }
            else
            {
                if (Data.Channels == 1)
                {
                    switch (Data.BitsPerSample)
                    {
                        case 8: MixU8_Mono_NoLoop(buffer, numSamples, leftVol, rightVol); break;
                        case 16: MixS16_Mono_NoLoop(buffer, numSamples, leftVol, rightVol); break;
                        default: MixF32_Mono_NoLoop(buffer, numSamples, leftVol, rightVol); break;
                    }
                }
                else
                {
                    switch (Data.BitsPerSample)
                    {
                        case 8: MixU8_Stereo_NoLoop(buffer, numSamples, leftVol, rightVol); break;
                        case 16: MixS16_Stereo_NoLoop(buffer, numSamples, leftVol, rightVol); break;
                        default: MixF32_Stereo_NoLoop(buffer, numSamples, leftVol, rightVol); break;
                    }
                }
            }
        }

        #region Fade

        public void BeginFade(float seconds, float from, float to)
        {
            _fadeDuration = seconds;
            _fadeFrom = from;
            _fadeTo = to;
            _fadeCurVolume = from;
            _fadeTime = 0f;
            IsFading = true;
        }
        public void ApplyFade(float[] buffer, int numSamples)
        {
            float fromVol = _fadeCurVolume;
            _fadeTime += SoundMixer.DeltaTime;
            float progress = _fadeTime / _fadeDuration;
            if (progress >= 1f)
            {
                progress = 1f;
                Volume = _fadeTo;
                IsFading = false;
            }
            float toVol = Utils.Lerp(_fadeFrom, _fadeTo, progress);
            float step = (toVol - fromVol) / numSamples;
            float level = fromVol;
            for (int j = 0; j < numSamples * 2; j += 2)
            {
                buffer[j] *= level;
                buffer[j + 1] *= level;
                level += step;
            }
            _fadeCurVolume = toVol;
        }

        #endregion

        #region U8 Mixing

        private void MixU8_Mono_NoLoop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _freq * SoundMixer.SAMPLE_RATE_RECIPROCAL;
            int bufPos = 0;
            do
            {
                Data.Stream.Position = _offset;
                int samp = Data.Reader.ReadByte();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                _offset += posDelta;

                buffer[bufPos] += SoundMixer.U8ToF32(samp, leftVol);
                buffer[bufPos + 1] += SoundMixer.U8ToF32(samp, rightVol);

                if (_offset >= Data.DataEnd)
                {
                    SoundMixer.StopChannel(this);
                    return;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixU8_Stereo_NoLoop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _freq * SoundMixer.SAMPLE_RATE_RECIPROCAL;
            int bufPos = 0;
            do
            {
                Data.Stream.Position = _offset;
                int sampL = Data.Reader.ReadByte();
                int sampR = Data.Reader.ReadByte();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= 2;
                _offset += posDelta;

                buffer[bufPos] += SoundMixer.U8ToF32(sampL, leftVol);
                buffer[bufPos + 1] += SoundMixer.U8ToF32(sampR, rightVol);

                if (_offset >= Data.DataEnd)
                {
                    SoundMixer.StopChannel(this);
                    return;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixU8_Mono_Loop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _freq * SoundMixer.SAMPLE_RATE_RECIPROCAL;
            int bufPos = 0;
            do
            {
                Data.Stream.Position = _offset;
                int samp = Data.Reader.ReadByte();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                _offset += posDelta;

                // Add trail
                if (_trailOffset < Data.DataEnd)
                {
                    Data.Stream.Position = _trailOffset;
                    samp += Data.Reader.ReadByte();
                    _trailOffset += posDelta;
                }

                buffer[bufPos] += SoundMixer.U8ToF32(samp, leftVol);
                buffer[bufPos + 1] += SoundMixer.U8ToF32(samp, rightVol);

                if (_offset >= Data.LoopEnd)
                {
                    _offset = Data.LoopStart;
                    _trailOffset = Data.LoopEnd;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixU8_Stereo_Loop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _freq * SoundMixer.SAMPLE_RATE_RECIPROCAL;
            int bufPos = 0;
            do
            {
                Data.Stream.Position = _offset;
                int sampL = Data.Reader.ReadByte();
                int sampR = Data.Reader.ReadByte();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= 2;
                _offset += posDelta;

                // Add trail
                if (_trailOffset < Data.DataEnd)
                {
                    Data.Stream.Position = _trailOffset;
                    sampL += Data.Reader.ReadByte();
                    sampR += Data.Reader.ReadByte();
                    _trailOffset += posDelta;
                }

                buffer[bufPos] += SoundMixer.U8ToF32(sampL, leftVol);
                buffer[bufPos + 1] += SoundMixer.U8ToF32(sampR, rightVol);

                if (_offset >= Data.LoopEnd)
                {
                    _offset = Data.LoopStart;
                    _trailOffset = Data.LoopEnd;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }

        #endregion

        #region S16 Mixing

        private void MixS16_Mono_NoLoop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _freq * SoundMixer.SAMPLE_RATE_RECIPROCAL;
            int bufPos = 0;
            do
            {
                Data.Stream.Position = _offset;
                int samp = Data.Reader.ReadInt16();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short);
                _offset += posDelta;

                buffer[bufPos] += SoundMixer.S16ToF32(samp, leftVol);
                buffer[bufPos + 1] += SoundMixer.S16ToF32(samp, rightVol);

                if (_offset >= Data.DataEnd)
                {
                    SoundMixer.StopChannel(this);
                    return;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixS16_Stereo_NoLoop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _freq * SoundMixer.SAMPLE_RATE_RECIPROCAL;
            int bufPos = 0;
            do
            {
                Data.Stream.Position = _offset;
                int sampL = Data.Reader.ReadInt16();
                int sampR = Data.Reader.ReadInt16();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short) * 2;
                _offset += posDelta;

                buffer[bufPos] += SoundMixer.S16ToF32(sampL, leftVol);
                buffer[bufPos + 1] += SoundMixer.S16ToF32(sampR, rightVol);

                if (_offset >= Data.DataEnd)
                {
                    SoundMixer.StopChannel(this);
                    return;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixS16_Mono_Loop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _freq * SoundMixer.SAMPLE_RATE_RECIPROCAL;
            int bufPos = 0;
            do
            {
                Data.Stream.Position = _offset;
                int samp = Data.Reader.ReadInt16();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short);
                _offset += posDelta;

                // Add trail
                if (_trailOffset < Data.DataEnd)
                {
                    Data.Stream.Position = _trailOffset;
                    samp += Data.Reader.ReadInt16();
                    _trailOffset += posDelta;
                }

                buffer[bufPos] += SoundMixer.S16ToF32(samp, leftVol);
                buffer[bufPos + 1] += SoundMixer.S16ToF32(samp, rightVol);

                if (_offset >= Data.LoopEnd)
                {
                    _offset = Data.LoopStart;
                    _trailOffset = Data.LoopEnd;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixS16_Stereo_Loop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _freq * SoundMixer.SAMPLE_RATE_RECIPROCAL;
            int bufPos = 0;
            do
            {
                Data.Stream.Position = _offset;
                int sampL = Data.Reader.ReadInt16();
                int sampR = Data.Reader.ReadInt16();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short) * 2;
                _offset += posDelta;

                // Add trail
                if (_trailOffset < Data.DataEnd)
                {
                    Data.Stream.Position = _trailOffset;
                    sampL += Data.Reader.ReadInt16();
                    sampR += Data.Reader.ReadInt16();
                    _trailOffset += posDelta;
                }

                buffer[bufPos] += SoundMixer.S16ToF32(sampL, leftVol);
                buffer[bufPos + 1] += SoundMixer.S16ToF32(sampR, rightVol);

                if (_offset >= Data.LoopEnd)
                {
                    _offset = Data.LoopStart;
                    _trailOffset = Data.LoopEnd;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }

        #endregion

        #region F32 Mixing

        private void MixF32_Mono_NoLoop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _freq * SoundMixer.SAMPLE_RATE_RECIPROCAL;
            int bufPos = 0;
            do
            {
                Data.Stream.Position = _offset;
                float samp = Data.Reader.ReadSingle();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(float);
                _offset += posDelta;

                buffer[bufPos] += samp * leftVol;
                buffer[bufPos + 1] += samp * rightVol;

                if (_offset >= Data.DataEnd)
                {
                    SoundMixer.StopChannel(this);
                    return;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixF32_Stereo_NoLoop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _freq * SoundMixer.SAMPLE_RATE_RECIPROCAL;
            int bufPos = 0;
            do
            {
                Data.Stream.Position = _offset;
                float sampL = Data.Reader.ReadSingle();
                float sampR = Data.Reader.ReadSingle();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(float) * 2;
                _offset += posDelta;

                buffer[bufPos] += sampL * leftVol;
                buffer[bufPos + 1] += sampR * rightVol;

                if (_offset >= Data.DataEnd)
                {
                    SoundMixer.StopChannel(this);
                    return;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixF32_Mono_Loop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _freq * SoundMixer.SAMPLE_RATE_RECIPROCAL;
            int bufPos = 0;
            do
            {
                Data.Stream.Position = _offset;
                float samp = Data.Reader.ReadSingle();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(float);
                _offset += posDelta;

                // Add trail
                if (_trailOffset < Data.DataEnd)
                {
                    Data.Stream.Position = _trailOffset;
                    samp += Data.Reader.ReadSingle();
                    _trailOffset += posDelta;
                }

                buffer[bufPos] += samp * leftVol;
                buffer[bufPos + 1] += samp * rightVol;

                if (_offset >= Data.LoopEnd)
                {
                    _offset = Data.LoopStart;
                    _trailOffset = Data.LoopEnd;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixF32_Stereo_Loop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _freq * SoundMixer.SAMPLE_RATE_RECIPROCAL;
            int bufPos = 0;
            do
            {
                Data.Stream.Position = _offset;
                float sampL = Data.Reader.ReadSingle();
                float sampR = Data.Reader.ReadSingle();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(float) * 2;
                _offset += posDelta;

                // Add trail
                if (_trailOffset < Data.DataEnd)
                {
                    Data.Stream.Position = _trailOffset;
                    sampL += Data.Reader.ReadSingle();
                    sampR += Data.Reader.ReadSingle();
                    _trailOffset += posDelta;
                }

                buffer[bufPos] += sampL * leftVol;
                buffer[bufPos + 1] += sampR * rightVol;

                if (_offset >= Data.LoopEnd)
                {
                    _offset = Data.LoopStart;
                    _trailOffset = Data.LoopEnd;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }

        #endregion

        public void Dispose()
        {
            Data.DeductReference();
            IsStopped = true;
        }
    }
}