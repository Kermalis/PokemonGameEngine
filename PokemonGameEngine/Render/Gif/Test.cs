using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XamlAnimatedGif.Decoding;
using XamlAnimatedGif.Decompression;

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
            GifDataStream gif;
            List<byte>[] decompressed;
            using (var r = new EndianBinaryReader(stream, encoding: EncodingType.UTF8))
            {
                gif = new GifDataStream(r);
                decompressed = new List<byte>[gif.Frames.Count];
                for (int i = 0; i < gif.Frames.Count; i++)
                {
                    GifFrame frame = gif.Frames[i];
                    r.BaseStream.Position = frame.ImageData.CompressedDataStartOffset;
                    using (var ms = new MemoryStream())
                    {
                        GifHelpers.CopyDataBlocksToStreamAsync(r, ms);
                        var lzwStream = new LzwDecompressStream(ms.GetBuffer(), frame.ImageData.LzwMinimumCodeSize);
                        decompressed[i] = lzwStream.Convert();
                    }
                }
            }
            return RenderFrames(gif, decompressed);
        }

        private static Sprite CreateSprite(GifDataStream gif)
        {
            GifLogicalScreenDescriptor desc = gif.Header.LogicalScreenDescriptor;
            return new Sprite(desc.Width, desc.Height);
        }
        private static uint[] FromGifColorTable(GifColor[] colors)
        {
            return colors.Select(gc => RenderUtils.ToRGBA8888(gc.R, gc.G, gc.B, 0xFF)).ToArray();
        }
        private static Dictionary<int, GifPalette> CreatePalettes(GifDataStream gif)
        {
            var palettes = new Dictionary<int, GifPalette>(gif.Frames.Count);
            uint[] globalColorTable = null;
            if (gif.Header.LogicalScreenDescriptor.HasGlobalColorTable)
            {
                globalColorTable = FromGifColorTable(gif.GlobalColorTable);
            }

            for (int i = 0; i < gif.Frames.Count; i++)
            {
                GifFrame frame = gif.Frames[i];
                uint[] colorTable = globalColorTable;
                if (frame.Descriptor.HasLocalColorTable)
                {
                    colorTable = FromGifColorTable(frame.LocalColorTable);
                }

                GifGraphicControlExtension gce = frame.GraphicControl;
                int? transparencyIndex = gce != null && gce.HasTransparency ? gce.TransparencyIndex : (int?)null;

                palettes.Add(i, new GifPalette(transparencyIndex, colorTable));
            }

            return palettes;
        }

        private static unsafe AnimatedSprite.Sprite RenderFrames(GifDataStream gif, List<byte>[] decompressed)
        {
            Sprite s = CreateSprite(gif);
            Dictionary<int, GifPalette> pals = CreatePalettes(gif);
            var outSprites = new AnimatedSprite.Frame[gif.Frames.Count];
            int prevFrameIndex = 0;
            GifFrame prevFrame = null;
            uint[] prevFrameBuffer = null;
            for (int frameIndex = 0; frameIndex < gif.Frames.Count; frameIndex++)
            {
                GifFrame frame = gif.Frames[frameIndex];
                GifImageDescriptor desc = frame.Descriptor;
                Int32Rect rect = GetFixedUpFrameRect(s, desc);
                DisposePreviousFrame(s, frame, prevFrame, ref prevFrameBuffer);

                List<byte> indexBuffer = decompressed[frameIndex];
                int indexBufferIndex = 0;

                GifPalette palette = pals[frameIndex];
                int transparencyIndex = palette.TransparencyIndex ?? -1;

                IEnumerable<int> rows = desc.Interlace ? InterlacedRows(rect.Height) : NormalRows(rect.Height);
                fixed (uint* bmpAddress = s.Bitmap)
                {
                    foreach (int y in rows)
                    {
                        for (int x = 0; x < rect.Width; x++)
                        {
                            byte colorIndex = indexBuffer[indexBufferIndex++];
                            if (colorIndex != transparencyIndex)
                            {
                                *RenderUtils.GetPixelAddress(bmpAddress, s.Width, x + desc.Left, y + desc.Top) = palette[colorIndex];
                            }
                        }
                    }
                }
                prevFrameIndex = frameIndex;
                prevFrame = frame;

                outSprites[frameIndex] = new AnimatedSprite.Frame(s.Bitmap, frame.GraphicControl?.Delay);
            }
            return new AnimatedSprite.Sprite(outSprites, s.Width, s.Height);
        }

        private static IEnumerable<int> NormalRows(int height)
        {
            return Enumerable.Range(0, height);
        }
        private static readonly (int Start, int Step)[] _passes = new[]
        {
            (0, 8),
            (4, 8),
            (2, 4),
            (1, 2),
        };
        private static IEnumerable<int> InterlacedRows(int height)
        {
            /*
             * 4 passes:
             * Pass 1: rows 0, 8, 16, 24...
             * Pass 2: rows 4, 12, 20, 28...
             * Pass 3: rows 2, 6, 10, 14...
             * Pass 4: rows 1, 3, 5, 7...
             * */
            foreach ((int start, int step) in _passes)
            {
                int y = start;
                while (y < height)
                {
                    yield return y;
                    y += step;
                }
            }
        }
        private static Int32Rect GetFixedUpFrameRect(Sprite sprite, GifImageDescriptor desc)
        {
            int width = Math.Min(desc.Width, sprite.Width - desc.Left);
            int height = Math.Min(desc.Height, sprite.Height - desc.Top);
            return new Int32Rect(desc.Left, desc.Top, width, height);
        }
        private static unsafe void DisposePreviousFrame(Sprite sprite, GifFrame currentFrame, GifFrame prevFrame, ref uint[] prevFrameBuffer)
        {
            GifGraphicControlExtension pgce = prevFrame?.GraphicControl;
            if (pgce != null)
            {
                switch (pgce.DisposalMethod)
                {
                    case GifFrameDisposalMethod.None:
                    case GifFrameDisposalMethod.DoNotDispose:
                    {
                        // Leave previous frame in place
                        break;
                    }
                    case GifFrameDisposalMethod.RestoreBackground:
                    {
                        fixed (uint* bmpAddress = sprite.Bitmap)
                        {
                            ClearArea(bmpAddress, sprite.Width, sprite.Height, GetFixedUpFrameRect(sprite, prevFrame.Descriptor));
                        }
                        break;
                    }
                    case GifFrameDisposalMethod.RestorePrevious:
                    {
                        sprite.Bitmap = (uint[])prevFrameBuffer.Clone();
                        break;
                    }
                }
            }

            GifGraphicControlExtension gce = currentFrame.GraphicControl;
            if (gce != null && gce.DisposalMethod == GifFrameDisposalMethod.RestorePrevious)
            {
                prevFrameBuffer = (uint[])sprite.Bitmap.Clone();
            }
        }
        private static unsafe void ClearArea(uint* bmpAddress, int bmpWidth, int bmpHeight, IGifRect rect)
        {
            RenderUtils.FillColor_Overwrite(bmpAddress, bmpWidth, bmpHeight, rect.Left, rect.Top, rect.Width, rect.Height, 0);
        }
        private static unsafe void ClearArea(uint* bmpAddress, int bmpWidth, int bmpHeight, Int32Rect rect)
        {
            RenderUtils.FillColor_Overwrite(bmpAddress, bmpWidth, bmpHeight, rect.X, rect.Y, rect.Width, rect.Height, 0);
        }
    }
}
