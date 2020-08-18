using Kermalis.EndianBinaryIO;

namespace XamlAnimatedGif.Decoding
{
    internal sealed class GifImageData
    {
        public byte LzwMinimumCodeSize { get; }
        public long CompressedDataStartOffset { get; }

        internal GifImageData(EndianBinaryReader r)
        {
            LzwMinimumCodeSize = r.ReadByte();
            CompressedDataStartOffset = r.BaseStream.Position;
            GifHelpers.ConsumeDataBlocksAsync(r);
        }
    }
}
