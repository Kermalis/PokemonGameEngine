using System.Runtime.CompilerServices;

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

        #region U8 Mixing

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MixU8Samples_Mono(float[] buffer, int index, long offset, float leftVol, float rightVol)
        {
            SoundMixer.MixU8Samples_Mono(buffer, index, _data.Reader, offset, leftVol, rightVol);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MixU8Samples_Stereo(float[] buffer, int index, long offset, float leftVol, float rightVol)
        {
            SoundMixer.MixU8Samples_Stereo(buffer, index, _data.Reader, offset, leftVol, rightVol);
        }

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
                MixU8Samples_Mono(buffer, bufPos, _offset, leftVol, rightVol);

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                _offset += posDelta;

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
                MixU8Samples_Stereo(buffer, bufPos, _offset, leftVol, rightVol);

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= 2;
                _offset += posDelta;

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
                MixU8Samples_Mono(buffer, bufPos, _offset, leftVol, rightVol);

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                _offset += posDelta;

                // Add trail
                if (_trailOffset < _data.DataEnd)
                {
                    MixU8Samples_Mono(buffer, bufPos, _trailOffset, leftVol, rightVol);
                    _trailOffset += posDelta;
                }

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
                MixU8Samples_Stereo(buffer, bufPos, _offset, leftVol, rightVol);

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= 2;
                _offset += posDelta;

                // Add trail
                if (_trailOffset < _data.DataEnd)
                {
                    MixU8Samples_Stereo(buffer, bufPos, _trailOffset, leftVol, rightVol);
                    _trailOffset += posDelta;
                }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MixS16Samples_Mono(float[] buffer, int index, long offset, float leftVol, float rightVol)
        {
            SoundMixer.MixS16Samples_Mono(buffer, index, _data.Reader, offset, leftVol, rightVol);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MixS16Samples_Stereo(float[] buffer, int index, long offset, float leftVol, float rightVol)
        {
            SoundMixer.MixS16Samples_Stereo(buffer, index, _data.Reader, offset, leftVol, rightVol);
        }

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
                MixS16Samples_Mono(buffer, bufPos, _offset, leftVol, rightVol);

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short);
                _offset += posDelta;

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
                MixS16Samples_Stereo(buffer, bufPos, _offset, leftVol, rightVol);

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short) * 2;
                _offset += posDelta;

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
                MixS16Samples_Mono(buffer, bufPos, _offset, leftVol, rightVol);

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short);
                _offset += posDelta;

                // Add trail
                if (_trailOffset < _data.DataEnd)
                {
                    MixS16Samples_Mono(buffer, bufPos, _trailOffset, leftVol, rightVol);
                    _trailOffset += posDelta;
                }

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
                MixS16Samples_Stereo(buffer, bufPos, _offset, leftVol, rightVol);

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short) * 2;
                _offset += posDelta;

                // Add trail
                if (_trailOffset < _data.DataEnd)
                {
                    MixS16Samples_Stereo(buffer, bufPos, _trailOffset, leftVol, rightVol);
                    _trailOffset += posDelta;
                }

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