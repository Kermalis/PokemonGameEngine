using Kermalis.EndianBinaryIO;

namespace XamlAnimatedGif.Decoding
{
    internal struct GifColor
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        internal GifColor(EndianBinaryReader r)
        {
            R = r.ReadByte();
            G = r.ReadByte();
            B = r.ReadByte();
        }

        public override string ToString()
        {
            return $"#{R:x2}{G:x2}{B:x2}";
        }
    }
}
