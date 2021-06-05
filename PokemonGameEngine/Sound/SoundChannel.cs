namespace Kermalis.PokemonGameEngine.Sound
{
    internal delegate void SoundStoppedFunc(SoundChannel c);
    internal sealed class SoundChannel
    {
        private const bool CheckPause = false; // Not needed because tasks are not parallel right now

        public SoundChannel Next;
        public SoundChannel Prev;

        public bool IsPaused;
        public float EffectVolume = 1f;
        public float Volume = 1f;
        //public float Panpot = 0f; // -1 left, 0 center, +1 right
        public SoundStoppedFunc OnStopped;

        public readonly WaveFileData Data;

        private float _interPos;
        private long _offset;
        private long _trailOffset;

        public SoundChannel(WaveFileData data)
        {
            Data = data;
            _offset = Data.DataStart;
            _trailOffset = Data.DataEnd;
        }

        private float GetLeftVol()
        {
            float lAmp = 1;// 1 - (Panpot / 2 + 0.5f);
            return EffectVolume * Volume * lAmp;
        }
        private float GetRightVol()
        {
            float rAmp = 1;// Panpot / 2 + 0.5f;
            return EffectVolume * Volume * rAmp;
        }

        public void MixF32(float[] buffer, int numSamples)
        {
            float leftVol = GetLeftVol();
            float rightVol = GetRightVol();
            if (Data.DoesLoop)
            {
                if (Data.Channels == 1)
                {
                    if (Data.BitsPerSample == 8)
                    {
                        MixU8_Mono_Loop(buffer, numSamples, leftVol, rightVol);
                    }
                    else
                    {
                        MixS16_Mono_Loop(buffer, numSamples, leftVol, rightVol);
                    }
                }
                else
                {
                    if (Data.BitsPerSample == 8)
                    {
                        MixU8_Stereo_Loop(buffer, numSamples, leftVol, rightVol);
                    }
                    else
                    {
                        MixS16_Stereo_Loop(buffer, numSamples, leftVol, rightVol);
                    }
                }
            }
            else
            {
                if (Data.Channels == 1)
                {
                    if (Data.BitsPerSample == 8)
                    {
                        MixU8_Mono_NoLoop(buffer, numSamples, leftVol, rightVol);
                    }
                    else
                    {
                        MixS16_Mono_NoLoop(buffer, numSamples, leftVol, rightVol);
                    }
                }
                else
                {
                    if (Data.BitsPerSample == 8)
                    {
                        MixU8_Stereo_NoLoop(buffer, numSamples, leftVol, rightVol);
                    }
                    else
                    {
                        MixS16_Stereo_NoLoop(buffer, numSamples, leftVol, rightVol);
                    }
                }
            }
        }

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
    }
}