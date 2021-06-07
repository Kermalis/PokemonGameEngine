using Kermalis.PokemonGameEngine.Util;
using System;
using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Sound
{
    internal delegate void SoundStoppedFunc(SoundChannel c);
    internal sealed class SoundChannel
    {
        private const bool CheckPause = false; // Not needed because tasks are not parallel right now

        public SoundChannel Next;
        public SoundChannel Prev;

        public bool IsPaused;
        public float Volume = 1f;
        /// <summary>-1 left, 0 center, +1 right</summary>
        public float Panpot = 0f;
        /// <summary>Callback for the sound stopping. This thread is not the Logic Thread</summary>
        public SoundStoppedFunc OnStopped;

        public readonly WaveFileData Data;

        // Playback
        private float _interPos;
        private long _offset;
        private long _trailOffset;

        // Fade
        public bool IsFading;
        private TimeSpan _fadeCurTime;
        private TimeSpan _fadeEndTime;
        private float _fadeCurVolume;
        private float _fadeFrom;
        private float _fadeTo;

        public SoundChannel(WaveFileData data)
        {
            Data = data;
            _offset = Data.DataStart;
            _trailOffset = Data.DataEnd;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetFadeVolume(double progress)
        {
            return (float)(_fadeFrom + ((_fadeTo - _fadeFrom) * progress));
        }

        public void BeginFade(int milliseconds, float from, float to)
        {
            if (IsFading)
            {
                _fadeFrom = _fadeCurVolume;
                _fadeEndTime = TimeSpan.FromMilliseconds(milliseconds) - _fadeCurTime;
            }
            else
            {
                _fadeFrom = from;
                _fadeCurVolume = from;
                _fadeEndTime = TimeSpan.FromMilliseconds(milliseconds);
                IsFading = true;
            }
            _fadeCurTime = new TimeSpan();
            _fadeTo = to;
        }
        public void ApplyFade(float[] buffer, int numSamples)
        {
            float fromVol = _fadeCurVolume;
            _fadeCurTime += SoundMixer.TimeSinceLastRender;
            double progress = Utils.GetProgress(_fadeEndTime, _fadeCurTime);
            float toVol = GetFadeVolume(progress);
            float step = (toVol - fromVol) / numSamples;
            float level = fromVol;
            for (int j = 0; j < numSamples; j++)
            {
                buffer[j * 2] *= level;
                buffer[(j * 2) + 1] *= level;
                level += step;
            }
            _fadeCurVolume = toVol;
            if (progress >= 1) // Fade is finished
            {
                Volume = toVol;
                IsFading = false;
            }
        }

        #endregion

        #region U8 Mixing

        private void MixU8_Mono_NoLoop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = Data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

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
            float interStep = Data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

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
            float interStep = Data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

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
            float interStep = Data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

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
            float interStep = Data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

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
            float interStep = Data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

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
            float interStep = Data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

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
            float interStep = Data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

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
            float interStep = Data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

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
            float interStep = Data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

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
            float interStep = Data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

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
            float interStep = Data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

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
    }
}