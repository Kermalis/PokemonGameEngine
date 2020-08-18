using Kermalis.EndianBinaryIO;

namespace XamlAnimatedGif.Decoding
{
    internal sealed class GifLogicalScreenDescriptor : IGifRect
    {
        public int Width { get; }
        public int Height { get; }
        public bool HasGlobalColorTable { get; }
        public int ColorResolution { get; }
        public bool IsGlobalColorTableSorted { get; }
        public int GlobalColorTableSize { get; }
        public int BackgroundColorIndex { get; }
        public double PixelAspectRatio { get; }

        int IGifRect.Left => 0;
        int IGifRect.Top => 0;

        internal GifLogicalScreenDescriptor(EndianBinaryReader r)
        {
            Width = r.ReadUInt16();
            Height = r.ReadUInt16();
            byte b = r.ReadByte();
            HasGlobalColorTable = (b & 0x80) != 0;
            ColorResolution = ((b & 0x70) >> 4) + 1;
            IsGlobalColorTableSorted = (b & 0x08) != 0;
            GlobalColorTableSize = 1 << ((b & 0x07) + 1);
            BackgroundColorIndex = r.ReadByte();
            b = r.ReadByte();
            PixelAspectRatio = b == 0 ? 0d : (15 + b) / 64d;
        }
    }
}
