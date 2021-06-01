//This file is adapted from NAudio (https://github.com/naudio/NAudio) which uses the MIT license
using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.PokemonGameEngine.Sound
{
    internal enum WaveFormatEncoding : ushort
    {
        Unknown = 0x0000,
        PCM = 0x0001,
        Adpcm = 0x0002,
        IeeeFloat = 0x0003,
    }
    internal sealed class WaveFileData
    {
        public readonly long DataStart;
        public readonly long DataEnd;

        public readonly short Channels;
        public readonly int SampleRate;
        public readonly short BitsPerSample;

        public readonly bool DoesLoop;
        public readonly long LoopStart;
        public readonly long LoopEnd;

        public readonly Stream Stream;
        public readonly EndianBinaryReader Reader;

        public WaveFileData(Stream stream)
        {
            Stream = stream;
            var r = new EndianBinaryReader(stream);
            Reader = r;

            DataStart = -1;
            long dataChunkLength = 0;

            string header = r.ReadString(4, false);
            bool isRf64 = false;
            if (header == "RF64")
            {
                isRf64 = true;
            }
            else if (header != "RIFF")
            {
                throw new FormatException("Not a WAVE file - no RIFF header");
            }
            long riffSize = r.ReadUInt32(); // Read the file size (minus 8 bytes)

            if (r.ReadString(4, false) != "WAVE")
            {
                throw new FormatException("Not a WAVE file - no WAVE header");
            }

            /// http://tech.ebu.ch/docs/tech/tech3306-2009.pdf
            if (isRf64)
            {
                if (r.ReadString(4, false) != "ds64")
                {
                    throw new FormatException("Invalid RF64 WAV file - No ds64 chunk found");
                }
                int chunkSize = r.ReadInt32();
                riffSize = r.ReadInt64();
                dataChunkLength = r.ReadInt64();
                _ = r.ReadInt64(); // sampleCount
                stream.Position += chunkSize - 24;
            }

            // sometimes a file has more data than is specified after the RIFF header
            long stopPosition = Math.Min(riffSize + 8, stream.Length);

            // this -8 is so we can be sure that there are at least 8 bytes for a chunk id and length
            while (stream.Position <= stopPosition - 8)
            {
                string chunkIdentifier = r.ReadString(4, false);
                uint chunkLength = r.ReadUInt32();
                if (chunkIdentifier == "data")
                {
                    DataStart = stream.Position;
                    if (!isRf64) // We already know the dataChunkLength if this is an RF64 file
                    {
                        dataChunkLength = chunkLength;
                    }
                    DataEnd = DataStart + dataChunkLength;
                    stream.Position += chunkLength;
                }
                else if (chunkIdentifier == "fmt ")
                {
                    if (chunkLength > int.MaxValue)
                    {
                        throw new InvalidDataException(string.Format("Format chunk length must be between 0 and {0}.", int.MaxValue));
                    }
                    int formatChunkLength = (int)chunkLength;
                    if (formatChunkLength < 16)
                    {
                        throw new InvalidDataException("Invalid WaveFormat Structure");
                    }
                    WaveFormatEncoding format = r.ReadEnum<WaveFormatEncoding>();
                    if (format != WaveFormatEncoding.PCM)
                    {
                        throw new InvalidDataException("Only PCM is supported");
                    }
                    Channels = r.ReadInt16();
                    if (Channels is not 1 and not 2)
                    {
                        throw new InvalidDataException("Only mono and stereo are supported");
                    }
                    SampleRate = r.ReadInt32();
                    _ = r.ReadInt32(); //averageBytesPerSecond
                    _ = r.ReadInt16(); // blockAlign
                    BitsPerSample = r.ReadInt16();
                    if (BitsPerSample is not 8 and not 16)
                    {
                        throw new InvalidDataException("Only PCM8 and PCM16 are supported");
                    }
                    if (formatChunkLength > 16)
                    {
                        short extraSize = r.ReadInt16();
                        if (extraSize != formatChunkLength - 18)
                        {
#if DEBUG
                            Console.WriteLine("Format chunk mismatch");
#endif
                            extraSize = (short)(formatChunkLength - 18);
                        }
                        if (extraSize > 0)
                        {
                            r.BaseStream.Position += extraSize;
                        }
                    }
                }
                else if (chunkIdentifier == "smpl")
                {
                    if (chunkLength is not 36 and not 60)
                    {
                        throw new InvalidDataException("Unsupported sample chunk size");
                    }
                    _ = r.ReadUInt32(); // 4 - manufacturer (0)
                    _ = r.ReadUInt32(); // 4 - product (0)
                    _ = r.ReadUInt32(); // 4 - sample period (0x5161)
                    _ = r.ReadUInt32(); // 4 - midi unity note (60)
                    _ = r.ReadUInt32(); // 4 - midi pitch fraction (0)
                    _ = r.ReadUInt32(); // 4 - SMPTE format (0)
                    _ = r.ReadUInt32(); // 4 - SMPTE offset (0)
                    uint numLoops = r.ReadUInt32(); // 4 - num sample loops (1)
                    if (numLoops is not 0 and not 1)
                    {
                        throw new InvalidDataException("Unsupported number of loop points");
                    }
                    if (numLoops == 1)
                    {
                        DoesLoop = true;
                    }
                    _ = r.ReadUInt32(); // 4 - sampler data (0)
                    if (DoesLoop)
                    {
                        _ = r.ReadUInt32(); // 4 - cue point ID (0x20000)
                        _ = r.ReadUInt32(); // 4 - type (0x400 for FL Studio, 0 for Edison)
                        LoopStart = r.ReadUInt32(); // 4 - loop start
                        LoopEnd = r.ReadUInt32(); // 4 - loop end
                        _ = r.ReadUInt32(); // 4 - fraction (0)
                        _ = r.ReadUInt32(); // 4 - play count (0)

                        // Adjust loop positions from samples to file offset
                        int i = Channels * (BitsPerSample == 8 ? sizeof(byte) : sizeof(short));
                        LoopStart = DataStart + (LoopStart * i);
                        LoopEnd = DataStart + (LoopEnd * i);
                    }
                }
                else // Skip other chunks
                {
                    stream.Position += chunkLength;
                }

                // All Chunks have to be word aligned.
                // https://www.tactilemedia.com/info/MCI_Control_Info.html
                // "If the chunk size is an odd number of bytes, a pad byte with value zero is
                //  written after ckData. Word aligning improves access speed (for chunks resident in memory)
                //  and maintains compatibility with EA IFF. The ckSize value does not include the pad byte."
                if (((chunkLength % 2) != 0) && (r.PeekByte() == 0))
                {
                    stream.Position++;
                }
            }

            if (SampleRate == 0)
            {
                throw new FormatException("Invalid WAV file - No fmt chunk found");
            }
            if (DataStart == -1)
            {
                throw new FormatException("Invalid WAV file - No data chunk found");
            }
        }
    }
}
