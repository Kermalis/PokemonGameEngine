using Kermalis.EndianBinaryIO;

namespace XamlAnimatedGif.Decoding
{
    internal sealed class GifGraphicControlExtension : GifExtension
    {
        internal const int ExtensionLabel = 0xF9;

        public int BlockSize { get; }
        public GifFrameDisposalMethod DisposalMethod { get; }
        public bool UserInput { get; }
        public bool HasTransparency { get; }
        public int Delay { get; } // In milliseconds, 0 means forever, no GraphicControlExtension means 100
        public int TransparencyIndex { get; }

        internal GifGraphicControlExtension(EndianBinaryReader r)
        {
            BlockSize = r.ReadByte(); // should always be 4
            if (BlockSize != 4)
            {
                throw GifHelpers.InvalidBlockSizeException("Graphic Control Extension", 4, BlockSize);
            }
            byte packedFields = r.ReadByte();
            DisposalMethod = (GifFrameDisposalMethod)((packedFields & 0x1C) >> 2);
            UserInput = (packedFields & 0x02) != 0;
            HasTransparency = (packedFields & 0x01) != 0;
            Delay = r.ReadUInt16() * 10;
            TransparencyIndex = r.ReadByte();
            r.ReadByte(); // Block terminator
        }

        internal override GifBlockKind Kind => GifBlockKind.Control;
    }
}
