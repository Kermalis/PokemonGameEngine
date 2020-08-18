using Kermalis.EndianBinaryIO;
using System.Collections.Generic;

namespace XamlAnimatedGif.Decoding
{
    internal abstract class GifBlock
    {
        internal abstract GifBlockKind Kind { get; }

        internal static GifBlock ReadAsync(EndianBinaryReader r, IEnumerable<GifExtension> controlExtensions)
        {
            byte blockId = r.ReadByte();
            switch (blockId)
            {
                case GifExtension.ExtensionIntroducer: return GifExtension.ReadAsync(r, controlExtensions);
                case GifFrame.ImageSeparator: return new GifFrame(r, controlExtensions);
                case GifTrailer.TrailerByte: return new GifTrailer();
                default: throw GifHelpers.UnknownBlockTypeException(blockId);
            }
        }
    }
}
