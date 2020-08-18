using System.Collections.Generic;

namespace XamlAnimatedGif.Decompression
{
    internal sealed class LzwDecompressStream
    {
        private const int MaxCodeLength = 12;
        private readonly BitReader _reader;
        private readonly CodeTable _codeTable;
        private int _prevCode;

        public LzwDecompressStream(byte[] compressedBuffer, int minimumCodeLength)
        {
            _reader = new BitReader(compressedBuffer);
            _codeTable = new CodeTable(minimumCodeLength);
        }

        public List<byte> Convert()
        {
            var buffer = new List<byte>();
            while (true)
            {
                int code = _reader.ReadBits(_codeTable.CodeLength);
                if (!ProcessCode(code, buffer))
                {
                    break;
                }
            }
            return buffer;
        }

        private void InitCodeTable()
        {
            _codeTable.Reset();
            _prevCode = -1;
        }

        private static void CopySequenceToBuffer(byte[] sequence, List<byte> buffer)
        {
            buffer.AddRange(sequence);
        }

        private bool ProcessCode(int code, List<byte> buffer)
        {
            if (code < _codeTable.Count)
            {
                Sequence sequence = _codeTable[code];
                if (sequence.IsStopCode)
                {
                    return false;
                }
                if (sequence.IsClearCode)
                {
                    InitCodeTable();
                    return true;
                }
                CopySequenceToBuffer(sequence.Bytes, buffer);
                if (_prevCode >= 0)
                {
                    Sequence prev = _codeTable[_prevCode];
                    Sequence newSequence = prev.Append(sequence.Bytes[0]);
                    _codeTable.Add(newSequence);
                }
            }
            else
            {
                Sequence prev = _codeTable[_prevCode];
                Sequence newSequence = prev.Append(prev.Bytes[0]);
                _codeTable.Add(newSequence);
                CopySequenceToBuffer(newSequence.Bytes, buffer);
            }
            _prevCode = code;
            return true;
        }

        private struct Sequence
        {
            public static Sequence ClearCode { get; } = new Sequence(true, false);
            public static Sequence StopCode { get; } = new Sequence(false, true);

            public byte[] Bytes { get; }
            public bool IsClearCode { get; }
            public bool IsStopCode { get; }

            public Sequence(byte[] bytes)
                : this()
            {
                Bytes = bytes;
            }

            private Sequence(bool isClearCode, bool isStopCode)
                : this()
            {
                IsClearCode = isClearCode;
                IsStopCode = isStopCode;
            }

            public Sequence Append(byte b)
            {
                byte[] bytes = new byte[Bytes.Length + 1];
                Bytes.CopyTo(bytes, 0);
                bytes[Bytes.Length] = b;
                return new Sequence(bytes);
            }
        }

        private sealed class CodeTable
        {
            private readonly int _minimumCodeLength;
            private readonly Sequence[] _table;

            public Sequence this[int index] => _table[index];
            public int Count { get; private set; }
            public int CodeLength { get; private set; }

            public CodeTable(int minimumCodeLength)
            {
                _minimumCodeLength = minimumCodeLength;
                CodeLength = _minimumCodeLength + 1;
                int initialEntries = 1 << minimumCodeLength;
                _table = new Sequence[1 << MaxCodeLength];
                for (int i = 0; i < initialEntries; i++)
                {
                    _table[Count++] = new Sequence(new[] { (byte)i });
                }
                Add(Sequence.ClearCode);
                Add(Sequence.StopCode);
            }
            public void Reset()
            {
                Count = (1 << _minimumCodeLength) + 2;
                CodeLength = _minimumCodeLength + 1;
            }

            public void Add(Sequence sequence)
            {
                // Code table is full, stop adding new codes
                if (Count >= _table.Length)
                {
                    return;
                }
                _table[Count++] = sequence;
                if ((Count & (Count - 1)) == 0 && CodeLength < MaxCodeLength)
                {
                    CodeLength++;
                }
            }
        }
    }
}
