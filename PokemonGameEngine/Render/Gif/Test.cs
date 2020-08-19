using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Util;
using Kermalis.SimpleGIF;
using System.IO;

namespace Kermalis.PokemonGameEngine.Render.Gif
{
    internal static class Test
    {
        private sealed class GifPalette
        {
            private readonly uint[] _colors;
            public int? TransparencyIndex { get; }
            public uint this[int i] => _colors[i];

            public GifPalette(int? transparencyIndex, uint[] colors)
            {
                TransparencyIndex = transparencyIndex;
                _colors = colors;
            }
        }
        private struct Int32Rect
        {
            public int X { get; }
            public int Y { get; }
            public int Width { get; }
            public int Height { get; }

            public Int32Rect(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }
        }

        public static AnimatedSprite.Sprite TestIt()
        {
            return TestIt(SpriteUtils.GetPokemonSpriteResource(PBESpecies.Giratina, PBEForm.Giratina_Origin, PBEGender.Genderless, false, false, false));
            //return TestIt(File.OpenRead(@"C:\Users\Kermalis\Documents\Development\GitHub\AvaloniaGif\AvaloniaGif.Demo\Images\c-loader.gif"));
        }
        public static AnimatedSprite.Sprite TestIt(string resource)
        {
            return TestIt(Utils.GetResourceStream(resource));
        }
        public static AnimatedSprite.Sprite TestIt(Stream stream)
        {
            return new AnimatedSprite.Sprite(GIFRenderer.DecodeAllFrames(stream, SimpleGIF.Decoding.ColorFormat.RGBA));
        }
    }
}
