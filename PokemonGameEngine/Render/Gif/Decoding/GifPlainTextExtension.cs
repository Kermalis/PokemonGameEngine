using Kermalis.EndianBinaryIO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace XamlAnimatedGif.Decoding
{
    internal sealed class GifPlainTextExtension : GifExtension
    {
        internal const int ExtensionLabel = 0x01;

        public int BlockSize { get; }
        public int Left { get; }
        public int Top { get; }
        public int Width { get; }
        public int Height { get; }
        public int CellWidth { get; }
        public int CellHeight { get; }
        public int ForegroundColorIndex { get; }
        public int BackgroundColorIndex { get; }
        public string Text { get; }

        public ReadOnlyCollection<GifExtension> Extensions { get; }

        internal GifPlainTextExtension(EndianBinaryReader r, IEnumerable<GifExtension> controlExtensions)
        {
            BlockSize = r.ReadByte();
            if (BlockSize != 12)
            {
                throw GifHelpers.InvalidBlockSizeException("Plain Text Extension", 12, BlockSize);
            }
            Left = r.ReadUInt16();
            Top = r.ReadUInt16();
            Width = r.ReadUInt16();
            Height = r.ReadUInt16();
            CellWidth = r.ReadByte();
            CellHeight = r.ReadByte();
            ForegroundColorIndex = r.ReadByte();
            BackgroundColorIndex = r.ReadByte();

            byte[] dataBytes = GifHelpers.ReadDataBlocksAsync(r);
            Text = GifHelpers.GetString(dataBytes);
            Extensions = controlExtensions.ToList().AsReadOnly();
        }

        internal override GifBlockKind Kind => GifBlockKind.GraphicRendering;
    }
}
