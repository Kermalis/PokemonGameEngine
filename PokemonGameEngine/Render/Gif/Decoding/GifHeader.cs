using Kermalis.EndianBinaryIO;

namespace XamlAnimatedGif.Decoding
{
    internal sealed class GifHeader : GifBlock
    {
        public string Signature { get; }
        public string Version { get; }
        public GifLogicalScreenDescriptor LogicalScreenDescriptor { get; }

        internal GifHeader(EndianBinaryReader r)
        {
            Signature = r.ReadString(3);
            if (Signature != "GIF")
            {
                throw GifHelpers.InvalidSignatureException(Signature);
            }
            Version = r.ReadString(3);
            if (Version != "87a" && Version != "89a")
            {
                throw GifHelpers.UnsupportedVersionException(Version);
            }
            LogicalScreenDescriptor = new GifLogicalScreenDescriptor(r);
        }

        internal override GifBlockKind Kind => GifBlockKind.Other;
    }
}
