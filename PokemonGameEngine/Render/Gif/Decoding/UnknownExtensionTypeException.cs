using System;

namespace XamlAnimatedGif.Decoding
{
    public sealed class UnknownExtensionTypeException : GifDecoderException
    {
        internal UnknownExtensionTypeException(string message) : base(message) { }
        internal UnknownExtensionTypeException(string message, Exception inner) : base(message, inner) { }
    }
}