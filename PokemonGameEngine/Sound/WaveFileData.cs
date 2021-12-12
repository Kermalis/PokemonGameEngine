//This file is adapted from NAudio (https://github.com/naudio/NAudio) which uses the MIT license
using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using System;
using System.Collections.Generic;
using System.IO;
#if DEBUG
using Kermalis.PokemonGameEngine.Debug;
#endif

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

        public readonly FileStream Stream;
        public readonly EndianBinaryReader Reader;

        private WaveFileData(string asset)
        {
            _id = asset;
            _numReferences = 1;
            _dataCache.Add(asset, this);
            Stream = AssetLoader.GetAssetStream(asset);
            Reader = new EndianBinaryReader(Stream);

            DataStart = -1;
            long dataChunkLength = 0;

            string header = Reader.ReadString(4, false);
            bool isRf64 = false;
            if (header == "RF64")
            {
                isRf64 = true;
            }
            else if (header != "RIFF")
            {
                throw new FormatException("Not a WAVE file - no RIFF header");
            }
            long riffSize = Reader.ReadUInt32(); // Read the file size (minus 8 bytes)

            if (Reader.ReadString(4, false) != "WAVE")
            {
                throw new FormatException("Not a WAVE file - no WAVE header");
            }

            /// http://tech.ebu.ch/docs/tech/tech3306-2009.pdf
            if (isRf64)
            {
                if (Reader.ReadString(4, false) != "ds64")
                {
                    throw new FormatException("Invalid RF64 WAV file - No ds64 chunk found");
                }
                int chunkSize = Reader.ReadInt32();
                riffSize = Reader.ReadInt64();
                dataChunkLength = Reader.ReadInt64();
                _ = Reader.ReadInt64(); // sampleCount
                Stream.Position += chunkSize - 24;
            }

            // sometimes a file has more data than is specified after the RIFF header
            long stopPosition = Math.Min(riffSize + 8, Stream.Length);

            // this -8 is so we can be sure that there are at least 8 bytes for a chunk id and length
            while (Stream.Position <= stopPosition - 8)
            {
                string chunkIdentifier = Reader.ReadString(4, false);
                uint chunkLength = Reader.ReadUInt32();
                if (chunkIdentifier == "data")
                {
                    DataStart = Stream.Position;
                    if (!isRf64) // We already know the dataChunkLength if this is an RF64 file
                    {
                        dataChunkLength = chunkLength;
                    }
                    DataEnd = DataStart + dataChunkLength;
                    Stream.Position += chunkLength;
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
                    WaveFormatEncoding format = Reader.ReadEnum<WaveFormatEncoding>();
                    if (format is not WaveFormatEncoding.PCM and not WaveFormatEncoding.IeeeFloat)
                    {
                        throw new InvalidDataException("Only PCM8, PCM16, and IEEE32 are supported");
                    }
                    Channels = Reader.ReadInt16();
                    if (Channels is not 1 and not 2)
                    {
                        throw new InvalidDataException("Only mono and stereo are supported");
                    }
                    SampleRate = Reader.ReadInt32();
                    _ = Reader.ReadInt32(); //averageBytesPerSecond
                    _ = Reader.ReadInt16(); // blockAlign
                    BitsPerSample = Reader.ReadInt16();
                    if ((format == WaveFormatEncoding.PCM && BitsPerSample is not 8 and not 16)
                        || (format == WaveFormatEncoding.IeeeFloat && BitsPerSample != 32))
                    {
                        throw new InvalidDataException("Only PCM8, PCM16, and IEEE32 are supported");
                    }
                    if (formatChunkLength > 16)
                    {
                        short extraSize = Reader.ReadInt16();
                        if (extraSize != formatChunkLength - 18)
                        {
#if DEBUG
                            Log.WriteLine("Format chunk mismatch in " + asset);
#endif
                            extraSize = (short)(formatChunkLength - 18);
                        }
                        if (extraSize > 0)
                        {
                            Reader.BaseStream.Position += extraSize;
                        }
                    }
                }
                else if (chunkIdentifier == "smpl")
                {
                    if (chunkLength is not 36 and not 60)
                    {
                        throw new InvalidDataException("Unsupported sample chunk size");
                    }
                    _ = Reader.ReadUInt32(); // 4 - manufacturer (0)
                    _ = Reader.ReadUInt32(); // 4 - product (0)
                    _ = Reader.ReadUInt32(); // 4 - sample period (0x5161)
                    _ = Reader.ReadUInt32(); // 4 - midi unity note (60)
                    _ = Reader.ReadUInt32(); // 4 - midi pitch fraction (0)
                    _ = Reader.ReadUInt32(); // 4 - SMPTE format (0)
                    _ = Reader.ReadUInt32(); // 4 - SMPTE offset (0)
                    uint numLoops = Reader.ReadUInt32(); // 4 - num sample loops (1)
                    if (numLoops is not 0 and not 1)
                    {
                        throw new InvalidDataException("Unsupported number of loop points");
                    }
                    if (numLoops == 1)
                    {
                        DoesLoop = true;
                    }
                    _ = Reader.ReadUInt32(); // 4 - sampler data (0)
                    if (DoesLoop)
                    {
                        _ = Reader.ReadUInt32(); // 4 - cue point ID (0x20000)
                        _ = Reader.ReadUInt32(); // 4 - type (0x400 for FL Studio, 0 for Edison)
                        LoopStart = Reader.ReadUInt32(); // 4 - loop start
                        LoopEnd = Reader.ReadUInt32(); // 4 - loop end
                        _ = Reader.ReadUInt32(); // 4 - fraction (0)
                        _ = Reader.ReadUInt32(); // 4 - play count (0)

                        // Adjust loop positions from samples to file offset
                        int i = Channels * (BitsPerSample / 8);
                        LoopStart = DataStart + (LoopStart * i);
                        LoopEnd = DataStart + (LoopEnd * i);
                    }
                }
                else // Skip other chunks
                {
                    Stream.Position += chunkLength;
                }

                // All Chunks have to be word aligned.
                // https://www.tactilemedia.com/info/MCI_Control_Info.html
                // "If the chunk size is an odd number of bytes, a pad byte with value zero is
                //  written after ckData. Word aligning improves access speed (for chunks resident in memory)
                //  and maintains compatibility with EA IFF. The ckSize value does not include the pad byte."
                if (((chunkLength % 2) != 0) && (Reader.PeekByte() == 0))
                {
                    Stream.Position++;
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

        #region Cache

        private readonly string _id;
        private int _numReferences;
        private static readonly Dictionary<string, WaveFileData> _dataCache = new();

        public static WaveFileData Get(string asset)
        {
            if (_dataCache.TryGetValue(asset, out WaveFileData data))
            {
                data._numReferences++;
            }
            else
            {
                data = new WaveFileData(asset);
            }
            return data;
        }
        public void DeductReference()
        {
            if (--_numReferences <= 0)
            {
                Stream.Dispose();
                _dataCache.Remove(_id);
            }
        }

        #endregion
    }
}
