namespace Kermalis.PokemonGameEngine.Sound
{
    internal sealed class SoundChannel
    {
        private const bool CheckPause = false; // Not needed because tasks are not parallel right now

        public SoundChannel Next;
        public SoundChannel Prev;

        public bool IsPaused;
        public float EffectVolume = 1f;
        public float Volume = 1f;
        //public float Panpot = 0f; // -1 left, 0 center, +1 right

        private readonly WaveFileData _data;

        private float _interPos;
        private long _offset;
        private long _trailOffset;

        public SoundChannel(WaveFileData data)
        {
            _data = data;
            _offset = _data.DataStart;
            _trailOffset = _data.DataEnd;
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
            if (_data.DoesLoop)
            {
                if (_data.Channels == 1)
                {
                    if (_data.BitsPerSample == 8)
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
                    if (_data.BitsPerSample == 8)
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
                if (_data.Channels == 1)
                {
                    if (_data.BitsPerSample == 8)
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
                    if (_data.BitsPerSample == 8)
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
            float interStep = _data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

                _data.Stream.Position = _offset;
                int samp = _data.Reader.ReadByte();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                _offset += posDelta;

                SoundMixer.MixU8AndF32Sample(buffer, bufPos, samp, leftVol);
                SoundMixer.MixU8AndF32Sample(buffer, bufPos + 1, samp, rightVol);

                if (_offset >= _data.DataEnd)
                {
                    SoundMixer.StopSound(this);
                    return;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixU8_Stereo_NoLoop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

                _data.Stream.Position = _offset;
                int sampL = _data.Reader.ReadByte();
                int sampR = _data.Reader.ReadByte();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= 2;
                _offset += posDelta;

                SoundMixer.MixU8AndF32Sample(buffer, bufPos, sampL, leftVol);
                SoundMixer.MixU8AndF32Sample(buffer, bufPos + 1, sampR, rightVol);

                if (_offset >= _data.DataEnd)
                {
                    SoundMixer.StopSound(this);
                    return;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixU8_Mono_Loop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

                _data.Stream.Position = _offset;
                int samp = _data.Reader.ReadByte();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                _offset += posDelta;

                // Add trail
                if (_trailOffset < _data.DataEnd)
                {
                    _data.Stream.Position = _trailOffset;
                    samp += _data.Reader.ReadByte();
                    _trailOffset += posDelta;
                }

                SoundMixer.MixU8AndF32Sample(buffer, bufPos, samp, leftVol);
                SoundMixer.MixU8AndF32Sample(buffer, bufPos + 1, samp, rightVol);

                if (_offset >= _data.LoopEnd)
                {
                    _offset = _data.LoopStart;
                    _trailOffset = _data.LoopEnd;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixU8_Stereo_Loop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

                _data.Stream.Position = _offset;
                int sampL = _data.Reader.ReadByte();
                int sampR = _data.Reader.ReadByte();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= 2;
                _offset += posDelta;

                // Add trail
                if (_trailOffset < _data.DataEnd)
                {
                    _data.Stream.Position = _trailOffset;
                    sampL += _data.Reader.ReadByte();
                    sampR += _data.Reader.ReadByte();
                    _trailOffset += posDelta;
                }

                SoundMixer.MixU8AndF32Sample(buffer, bufPos, sampL, leftVol);
                SoundMixer.MixU8AndF32Sample(buffer, bufPos + 1, sampR, rightVol);

                if (_offset >= _data.LoopEnd)
                {
                    _offset = _data.LoopStart;
                    _trailOffset = _data.LoopEnd;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }

        #endregion

        #region S16 Mixing

        private void MixS16_Mono_NoLoop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

                _data.Stream.Position = _offset;
                int samp = _data.Reader.ReadInt16();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short);
                _offset += posDelta;

                SoundMixer.MixS16AndF32Sample(buffer, bufPos, samp, leftVol);
                SoundMixer.MixS16AndF32Sample(buffer, bufPos + 1, samp, rightVol);

                if (_offset >= _data.DataEnd)
                {
                    SoundMixer.StopSound(this);
                    return;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixS16_Stereo_NoLoop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

                _data.Stream.Position = _offset;
                int sampL = _data.Reader.ReadInt16();
                int sampR = _data.Reader.ReadInt16();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short) * 2;
                _offset += posDelta;

                SoundMixer.MixS16AndF32Sample(buffer, bufPos, sampL, leftVol);
                SoundMixer.MixS16AndF32Sample(buffer, bufPos + 1, sampR, rightVol);

                if (_offset >= _data.DataEnd)
                {
                    SoundMixer.StopSound(this);
                    return;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixS16_Mono_Loop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

                _data.Stream.Position = _offset;
                int samp = _data.Reader.ReadInt16();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short);
                _offset += posDelta;

                // Add trail
                if (_trailOffset < _data.DataEnd)
                {
                    _data.Stream.Position = _trailOffset;
                    samp += _data.Reader.ReadInt16();
                    _trailOffset += posDelta;
                }

                SoundMixer.MixS16AndF32Sample(buffer, bufPos, samp, leftVol);
                SoundMixer.MixS16AndF32Sample(buffer, bufPos + 1, samp, rightVol);

                if (_offset >= _data.LoopEnd)
                {
                    _offset = _data.LoopStart;
                    _trailOffset = _data.LoopEnd;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixS16_Stereo_Loop(float[] buffer, int numSamples, float leftVol, float rightVol)
        {
            float interStep = _data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                if (CheckPause && IsPaused)
                {
                    return;
                }

                _data.Stream.Position = _offset;
                int sampL = _data.Reader.ReadInt16();
                int sampR = _data.Reader.ReadInt16();

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short) * 2;
                _offset += posDelta;

                // Add trail
                if (_trailOffset < _data.DataEnd)
                {
                    _data.Stream.Position = _trailOffset;
                    sampL += _data.Reader.ReadInt16();
                    sampR += _data.Reader.ReadInt16();
                    _trailOffset += posDelta;
                }

                SoundMixer.MixS16AndF32Sample(buffer, bufPos, sampL, leftVol);
                SoundMixer.MixS16AndF32Sample(buffer, bufPos + 1, sampR, rightVol);

                if (_offset >= _data.LoopEnd)
                {
                    _offset = _data.LoopStart;
                    _trailOffset = _data.LoopEnd;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }

        #endregion
    }
}