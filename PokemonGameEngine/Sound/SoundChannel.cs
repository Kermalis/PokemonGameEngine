using System;
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

        // https://stackoverflow.com/a/25102339
        // This can be adapted for s8 as well (and for unsigned if the += and -= are removed)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MixS16(short[] buffer, int index, short sample)
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
                m = 2 * (a + b) - (a * b) / magic - (magic * 2);
            }

            if (m == magic * 2)
            {
                m--;
            }
            m -= magic;
            buffer[index] = (short)m;
        }

        public void Mix(short[] buffer, int numSamples)
        {
            float interStep = _data.SampleRate * SoundMixer.SampleRateReciprocal;
            int bufPos = 0;
            do
            {
                _data.Stream.Position = _offset;
                short lSamp = _data.Reader.ReadInt16();
                short rSamp = _data.Channels == 1 ? lSamp : _data.Reader.ReadInt16();

                MixS16(buffer, bufPos, lSamp);
                MixS16(buffer, bufPos + 1, rSamp);

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                posDelta *= sizeof(short) * _data.Channels;
                _offset += posDelta;

                // Add trail
                if (_trailOffset < _data.DataEnd)
                {
                    _data.Stream.Position = _trailOffset;
                    lSamp = _data.Reader.ReadInt16();
                    rSamp = _data.Channels == 1 ? lSamp : _data.Reader.ReadInt16();

                    MixS16(buffer, bufPos, lSamp);
                    MixS16(buffer, bufPos + 1, rSamp);

                    _trailOffset += posDelta;
                }

                if (_data.DoesLoop && _offset >= _data.LoopEnd)
                {
                    _offset = _data.LoopStart;
                    _trailOffset = _data.LoopEnd;
                }

                bufPos += 2;
            } while (--numSamples > 0);
        }
    }
}
