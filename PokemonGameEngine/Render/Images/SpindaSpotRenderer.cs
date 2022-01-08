using Kermalis.PokemonGameEngine.Core;
using Kermalis.SimpleGIF;

namespace Kermalis.PokemonGameEngine.Render.Images
{
    internal static unsafe class SpindaSpotRenderer
    {
        #region Spot Coordinates

        // First pair is right eye, second is left eye, third is right ear, fourth is left ear
        // Pairs point to the center of each monument (the nibbles 8 and 8 will refer to the exact points in this pair)
        private static readonly Vec2I[][] _spindaSpotCoordinates = new Vec2I[59][]
        {
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 01
            new Vec2I[] { new(21, 22), new(09, 21), new(26, 04), new(07, 02) }, // 02
            new Vec2I[] { new(22, 22), new(10, 21), new(27, 03), new(09, 01) }, // 03
            new Vec2I[] { new(23, 22), new(11, 21), new(26, 02), new(12, 01) }, // 04
            new Vec2I[] { new(23, 22), new(11, 21), new(26, 03), new(09, 02) }, // 05
            new Vec2I[] { new(23, 22), new(11, 21), new(26, 02), new(12, 01) }, // 06
            new Vec2I[] { new(22, 22), new(10, 21), new(27, 03), new(09, 01) }, // 07
            new Vec2I[] { new(21, 22), new(09, 21), new(26, 04), new(07, 02) }, // 08
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 09
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 10
            new Vec2I[] { new(20, 22), new(08, 21), new(26, 04), new(04, 02) }, // 11
            new Vec2I[] { new(19, 22), new(07, 21), new(25, 04), new(03, 02) }, // 12
            new Vec2I[] { new(20, 22), new(08, 21), new(26, 04), new(04, 02) }, // 13
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 14
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 15
            new Vec2I[] { new(21, 22), new(09, 21), new(26, 04), new(07, 02) }, // 16
            new Vec2I[] { new(22, 22), new(10, 21), new(27, 03), new(09, 01) }, // 17
            new Vec2I[] { new(23, 22), new(11, 21), new(26, 02), new(12, 01) }, // 18
            new Vec2I[] { new(23, 22), new(11, 21), new(26, 03), new(09, 02) }, // 19
            new Vec2I[] { new(23, 22), new(11, 21), new(26, 02), new(12, 01) }, // 20
            new Vec2I[] { new(22, 22), new(10, 21), new(27, 03), new(09, 01) }, // 21
            new Vec2I[] { new(21, 22), new(09, 21), new(26, 04), new(07, 02) }, // 22
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 23
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 24
            new Vec2I[] { new(20, 22), new(08, 21), new(26, 04), new(04, 02) }, // 25
            new Vec2I[] { new(19, 22), new(07, 21), new(25, 04), new(03, 02) }, // 26
            new Vec2I[] { new(20, 22), new(08, 21), new(26, 04), new(04, 02) }, // 27
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 28
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 29
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 30
            new Vec2I[] { new(20, 22), new(08, 21), new(26, 04), new(04, 02) }, // 31
            new Vec2I[] { new(20, 23), new(08, 22), new(26, 05), new(04, 03) }, // 32
            new Vec2I[] { new(19, 23), new(07, 22), new(25, 05), new(03, 03) }, // 33
            new Vec2I[] { new(18, 23), new(06, 22), new(24, 05), new(02, 03) }, // 34
            new Vec2I[] { new(18, 24), new(06, 23), new(24, 06), new(02, 04) }, // 35
            new Vec2I[] { new(17, 24), new(05, 23), new(23, 06), new(01, 04) }, // 36
            new Vec2I[] { new(16, 25), new(04, 24), new(22, 07), new(00, 05) }, // 37
            new Vec2I[] { new(17, 24), new(05, 23), new(23, 06), new(01, 04) }, // 38
            new Vec2I[] { new(18, 24), new(06, 23), new(24, 06), new(02, 04) }, // 39
            new Vec2I[] { new(18, 23), new(06, 22), new(24, 05), new(02, 03) }, // 40
            new Vec2I[] { new(19, 23), new(07, 22), new(25, 05), new(03, 03) }, // 41
            new Vec2I[] { new(20, 23), new(08, 22), new(26, 05), new(04, 03) }, // 42
            new Vec2I[] { new(20, 22), new(08, 21), new(26, 04), new(04, 02) }, // 43
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 44
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 45
            new Vec2I[] { new(21, 22), new(09, 21), new(26, 04), new(07, 02) }, // 46
            new Vec2I[] { new(22, 22), new(10, 21), new(27, 03), new(09, 01) }, // 47
            new Vec2I[] { new(23, 22), new(11, 21), new(26, 02), new(12, 01) }, // 48
            new Vec2I[] { new(23, 22), new(11, 21), new(26, 03), new(09, 02) }, // 49
            new Vec2I[] { new(23, 22), new(11, 21), new(26, 02), new(12, 01) }, // 50
            new Vec2I[] { new(22, 22), new(10, 21), new(27, 03), new(09, 01) }, // 51
            new Vec2I[] { new(21, 22), new(09, 21), new(26, 04), new(07, 02) }, // 52
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 53
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 54
            new Vec2I[] { new(20, 22), new(08, 21), new(26, 04), new(04, 02) }, // 55
            new Vec2I[] { new(19, 22), new(07, 21), new(25, 04), new(03, 02) }, // 56
            new Vec2I[] { new(20, 22), new(08, 21), new(26, 04), new(04, 02) }, // 57
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }, // 58
            new Vec2I[] { new(21, 22), new(09, 21), new(27, 04), new(05, 02) }  // 59
        };

        #endregion

        private static readonly bool[,] _rightEye;
        private static readonly bool[,] _leftEye;
        private static readonly bool[,] _rightEar;
        private static readonly bool[,] _leftEar;

        static SpindaSpotRenderer()
        {
            static bool[,] Load(string asset)
            {
                uint spotColor = UnsafeRenderer.RawColor(0, 255, 255, 255);

                uint[] bitmap = AssetLoader.GetAssetBitmap(asset, out Vec2I size);
                bool[,] arr = new bool[size.Y, size.X];
                fixed (uint* src = bitmap)
                {
                    Vec2I pos;
                    for (pos.Y = 0; pos.Y < size.Y; pos.Y++)
                    {
                        for (pos.X = 0; pos.X < size.X; pos.X++)
                        {
                            arr[pos.Y, pos.X] = *UnsafeRenderer.GetPixelAddress(src, size.X, pos) == spotColor;
                        }
                    }
                }
                return arr;
            }
            _rightEye = Load(@"Sprites\SpindaSpot_RightEye.png");
            _leftEye = Load(@"Sprites\SpindaSpot_LeftEye.png");
            _rightEar = Load(@"Sprites\SpindaSpot_RightEar.png");
            _leftEar = Load(@"Sprites\SpindaSpot_LeftEar.png");
        }

        private static (uint color, uint replacement)[] SpindaSpotColorsFromShininess(bool shiny)
        {
            uint color1 = UnsafeRenderer.RawColor(230, 214, 165, 255);
            uint color1Replacement = shiny ? UnsafeRenderer.RawColor(165, 206, 16, 255) : UnsafeRenderer.RawColor(239, 82, 74, 255);
            uint color2 = UnsafeRenderer.RawColor(206, 165, 115, 255);
            uint color2Replacement = shiny ? UnsafeRenderer.RawColor(123, 156, 0, 255) : UnsafeRenderer.RawColor(189, 74, 49, 255);
            return new[] { (color1, color1Replacement), (color2, color2Replacement) };
        }
        public static void Render(DecodedGIF img, uint pid, bool shiny)
        {
            (uint color, uint replacement)[] colors = SpindaSpotColorsFromShininess(shiny);

            var imgSize = new Vec2I(img.Width, img.Height);
            for (int i = 0; i < img.Frames.Count; i++)
            {
                Vec2I[] coords = _spindaSpotCoordinates[i];
                fixed (uint* dst = img.Frames[i].Bitmap)
                {
                    Render(dst, imgSize, pid, coords, colors);
                }
            }
        }
        private static void Render(uint* dst, Vec2I dstSize, uint pid, Vec2I[] coords, (uint color, uint replacement)[] colors)
        {
            byte b = (byte)((pid >> 24) & 0xFF);
            RenderSpot(dst, dstSize, _rightEye, coords[0], b, colors);
            b = (byte)((pid >> 16) & 0xFF);
            RenderSpot(dst, dstSize, _leftEye, coords[1], b, colors);
            b = (byte)((pid >> 8) & 0xFF);
            RenderSpot(dst, dstSize, _rightEar, coords[2], b, colors);
            b = (byte)(pid & 0xFF);
            RenderSpot(dst, dstSize, _leftEar, coords[3], b, colors);
        }

        private static void RenderSpot(uint* dst, Vec2I dstSize, bool[,] spot, Vec2I center, byte data, (uint color, uint replacement)[] colors)
        {
            int spotX = data & 0xF;
            int spotY = (data >> 4) & 0xF;
            int height = spot.GetLength(0);
            int width = spot.GetLength(1);
            Vec2I bmpPos;
            for (int py = 0; py < height; py++)
            {
                bmpPos.Y = center.Y - 8 + spotY + py;
                if (bmpPos.Y < 0 || bmpPos.Y >= dstSize.Y)
                {
                    continue;
                }
                for (int px = 0; px < width; px++)
                {
                    bmpPos.X = center.X - 8 + spotX + px;
                    if (bmpPos.X < 0 || bmpPos.X >= dstSize.X || !spot[py, px])
                    {
                        continue;
                    }
                    uint* dstPix = UnsafeRenderer.GetPixelAddress(dst, dstSize.X, bmpPos);
                    uint curColor = *dstPix;
                    for (int i = 0; i < colors.Length; i++)
                    {
                        (uint color, uint replacement) = colors[i];
                        if (curColor == color)
                        {
                            *dstPix = replacement;
                            break;
                        }
                    }
                }
            }
        }
    }
}
