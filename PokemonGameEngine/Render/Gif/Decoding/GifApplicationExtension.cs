using Kermalis.EndianBinaryIO;

namespace XamlAnimatedGif.Decoding
{
    internal sealed class GifApplicationExtension : GifExtension
    {
        internal const int ExtensionLabel = 0xFF;

        public int BlockSize { get; }
        public string ApplicationIdentifier { get; }
        public byte[] AuthenticationCode { get; }
        public byte[] Data { get; }

        internal GifApplicationExtension(EndianBinaryReader r)
        {
            BlockSize = r.ReadByte(); // should always be 11
            if (BlockSize != 11)
            {
                throw GifHelpers.InvalidBlockSizeException("Application Extension", 11, BlockSize);
            }
            ApplicationIdentifier = r.ReadString(8);
            AuthenticationCode = r.ReadBytes(3);
            Data = GifHelpers.ReadDataBlocksAsync(r);
        }

        internal override GifBlockKind Kind => GifBlockKind.SpecialPurpose;
    }
}
