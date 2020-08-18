using Kermalis.EndianBinaryIO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace XamlAnimatedGif.Decoding
{
    internal sealed class GifDataStream
    {
        public GifHeader Header { get; }
        public GifColor[] GlobalColorTable { get; }
        public ReadOnlyCollection<GifFrame> Frames { get; }
        public ReadOnlyCollection<GifExtension> Extensions { get; }
        public ushort RepeatCount { get; }

        internal GifDataStream(EndianBinaryReader r)
        {
            Header = new GifHeader(r);
            if (Header.LogicalScreenDescriptor.HasGlobalColorTable)
            {
                GlobalColorTable = GifHelpers.ReadColorTableAsync(r, Header.LogicalScreenDescriptor.GlobalColorTableSize);
            }

            // Read frames
            var frames = new List<GifFrame>();
            var controlExtensions = new List<GifExtension>();
            var specialExtensions = new List<GifExtension>();
            while (true)
            {
                try
                {
                    var block = GifBlock.ReadAsync(r, controlExtensions);

                    if (block.Kind == GifBlockKind.GraphicRendering)
                    {
                        controlExtensions = new List<GifExtension>();
                    }
                    if (block is GifFrame frame)
                    {
                        frames.Add(frame);
                    }
                    else if (block is GifExtension extension)
                    {
                        switch (extension.Kind)
                        {
                            case GifBlockKind.Control: controlExtensions.Add(extension); break;
                            case GifBlockKind.SpecialPurpose: specialExtensions.Add(extension); break;
                        }
                    }
                    else if (block is GifTrailer)
                    {
                        break;
                    }
                }
                // Follow the same approach as Firefox:
                // If we find extraneous data between blocks, just assume the stream
                // was successfully terminated if we have some successfully decoded frames
                // https://dxr.mozilla.org/firefox/source/modules/libpr0n/decoders/gif/nsGIFDecoder2.cpp#894-909
                catch (UnknownBlockTypeException) when (frames.Count > 0)
                {
                    break;
                }
            }

            Frames = frames.AsReadOnly();
            Extensions = specialExtensions.AsReadOnly();

            GifApplicationExtension netscapeExtension = Extensions.OfType<GifApplicationExtension>().FirstOrDefault(GifHelpers.IsNetscapeExtension);
            RepeatCount = netscapeExtension != null ? GifHelpers.GetRepeatCount(netscapeExtension) : (ushort)1;
        }
    }
}
