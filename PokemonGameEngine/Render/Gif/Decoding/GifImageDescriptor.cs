using Kermalis.EndianBinaryIO;

namespace XamlAnimatedGif.Decoding
{
    internal sealed class GifImageDescriptor : IGifRect
    {
        public int Left { get; }
        public int Top { get; }
        public int Width { get; }
        public int Height { get; }
        public bool HasLocalColorTable { get; }
        public bool Interlace { get; }
        public bool IsLocalColorTableSorted { get; }
        public int LocalColorTableSize { get; }

        internal GifImageDescriptor(EndianBinaryReader r)
        {
            Left = r.ReadUInt16();
            Top = r.ReadUInt16();
            Width = r.ReadUInt16();
            Height = r.ReadUInt16();
            byte packedFields = r.ReadByte();
            HasLocalColorTable = (packedFields & 0x80) != 0;
            Interlace = (packedFields & 0x40) != 0;
            IsLocalColorTableSorted = (packedFields & 0x20) != 0;
            LocalColorTableSize = 1 << ((packedFields & 0x07) + 1);
        }
    }
}
