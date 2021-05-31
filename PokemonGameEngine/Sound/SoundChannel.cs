using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Sound
{
    internal sealed class SoundChannel
    {
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

        #region S16 Mixing

        public void MixS16(short[] buffer, int numSamples)
        {
            if (_data.DoesLoop)
            {
                if (_data.Channels == 1)
                {
                    MixS16_Mono_Loop(buffer, numSamples);
                }
                else
                {
                    MixS16_Stereo_Loop(buffer, numSamples);
                }
            }
            else
            {
                if (_data.Channels == 1)
                {
                    MixS16_Mono_NoLoop(buffer, numSamples);
                }
                else
                {
                    MixS16_Stereo_NoLoop(buffer, numSamples);
                }
            }
        }

        // https://stackoverflow.com/a/25102339
        // This can be adapted for s8 as well (and for unsigned if the += and -= are removed)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MixS16Samples(short[] buffer, int index, short sample)
        {
            const int magic = short.MaxValue + 1;
            int a = buffer[index];
            int b = sample;
            int m;

            a += magic;
            b += magic;

            if ((a < magic) || (b < magic))
            {
                m = a * b / magic;
            }
            else
            {
                m = (2 * (a + b)) - (a * b / magic) - (magic * 2);
            }

            if (m == magic * 2)
            {
                m--;
            }
            m -= magic;
            buffer[index] = (short)m;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MixS16Samples_Mono(short[] buffer, int index, long offset)
        {
            _data.Stream.Position = offset;
            short samp = _data.Reader.ReadInt16();
            MixS16Samples(buffer, index, samp);
            MixS16Samples(buffer, index + 1, samp);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MixS16Samples_Stereo(short[] buffer, int index, long offset)
        {
            _data.Stream.Position = offset;
            MixS16Samples(buffer, index, _data.Reader.ReadInt16());
            MixS16Samples(buffer, index + 1, _data.Reader.ReadInt16());
        }

        private void MixS16_Mono_NoLoop(short[] buffer, int numSamples)
        {
            float interStep = _data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                MixS16Samples_Mono(buffer, bufPos, _offset);

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short);
                _offset += posDelta;

                if (_offset >= _data.DataEnd)
                {
                    return;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixS16_Stereo_NoLoop(short[] buffer, int numSamples)
        {
            float interStep = _data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                MixS16Samples_Stereo(buffer, bufPos, _offset);

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short) * 2;
                _offset += posDelta;

                if (_offset >= _data.DataEnd)
                {
                    return;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
        private void MixS16_Mono_Loop(short[] buffer, int numSamples)
        {
            float interStep = _data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                MixS16Samples_Mono(buffer, bufPos, _offset);

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short);
                _offset += posDelta;

                // Add trail
                if (_trailOffset < _data.DataEnd)
                {
                    MixS16Samples_Mono(buffer, bufPos, _trailOffset);

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
        private void MixS16_Stereo_Loop(short[] buffer, int numSamples)
        {
            float interStep = _data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                MixS16Samples_Stereo(buffer, bufPos, _offset);

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short) * 2;
                _offset += posDelta;

                // Add trail
                if (_trailOffset < _data.DataEnd)
                {
                    MixS16Samples_Stereo(buffer, bufPos, _trailOffset);

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
