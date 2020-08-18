using Kermalis.EndianBinaryIO;

namespace XamlAnimatedGif.Decoding
{
    internal sealed class GifCommentExtension : GifExtension
    {
        internal const int ExtensionLabel = 0xFE;

        public string Text { get; }

        internal GifCommentExtension(EndianBinaryReader r)
        {
            Text = GifHelpers.GetString(GifHelpers.ReadDataBlocksAsync(r));
        }

        internal override GifBlockKind Kind => GifBlockKind.SpecialPurpose;
    }
}
