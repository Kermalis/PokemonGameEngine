using Kermalis.SimpleGIF;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe class SpindaSpotRenderer
    {
        #region Spot Coordinates
        // First pair is right eye, second is left eye, third is right ear, fourth is left ear
        // Pairs point to the center of each monument (the nibbles 8 and 8 will refer to the exact points in this pair)
        private static readonly (int X, int Y)[][] _spindaSpotCoordinates = new (int, int)[59][]
        {
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 01
            new[] { (21, 22), (09, 21), (26, 04), (07, 02) }, // 02
            new[] { (22, 22), (10, 21), (27, 03), (09, 01) }, // 03
            new[] { (23, 22), (11, 21), (26, 02), (12, 01) }, // 04
            new[] { (23, 22), (11, 21), (26, 03), (09, 02) }, // 05
            new[] { (23, 22), (11, 21), (26, 02), (12, 01) }, // 06
            new[] { (22, 22), (10, 21), (27, 03), (09, 01) }, // 07
            new[] { (21, 22), (09, 21), (26, 04), (07, 02) }, // 08
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 09
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 10
            new[] { (20, 22), (08, 21), (26, 04), (04, 02) }, // 11
            new[] { (19, 22), (07, 21), (25, 04), (03, 02) }, // 12
            new[] { (20, 22), (08, 21), (26, 04), (04, 02) }, // 13
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 14
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 15
            new[] { (21, 22), (09, 21), (26, 04), (07, 02) }, // 16
            new[] { (22, 22), (10, 21), (27, 03), (09, 01) }, // 17
            new[] { (23, 22), (11, 21), (26, 02), (12, 01) }, // 18
            new[] { (23, 22), (11, 21), (26, 03), (09, 02) }, // 19
            new[] { (23, 22), (11, 21), (26, 02), (12, 01) }, // 20
            new[] { (22, 22), (10, 21), (27, 03), (09, 01) }, // 21
            new[] { (21, 22), (09, 21), (26, 04), (07, 02) }, // 22
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 23
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 24
            new[] { (20, 22), (08, 21), (26, 04), (04, 02) }, // 25
            new[] { (19, 22), (07, 21), (25, 04), (03, 02) }, // 26
            new[] { (20, 22), (08, 21), (26, 04), (04, 02) }, // 27
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 28
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 29
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 30
            new[] { (20, 22), (08, 21), (26, 04), (04, 02) }, // 31
            new[] { (20, 23), (08, 22), (26, 05), (04, 03) }, // 32
            new[] { (19, 23), (07, 22), (25, 05), (03, 03) }, // 33
            new[] { (18, 23), (06, 22), (24, 05), (02, 03) }, // 34
            new[] { (18, 24), (06, 23), (24, 06), (02, 04) }, // 35
            new[] { (17, 24), (05, 23), (23, 06), (01, 04) }, // 36
            new[] { (16, 25), (04, 24), (22, 07), (00, 05) }, // 37
            new[] { (17, 24), (05, 23), (23, 06), (01, 04) }, // 38
            new[] { (18, 24), (06, 23), (24, 06), (02, 04) }, // 39
            new[] { (18, 23), (06, 22), (24, 05), (02, 03) }, // 40
            new[] { (19, 23), (07, 22), (25, 05), (03, 03) }, // 41
            new[] { (20, 23), (08, 22), (26, 05), (04, 03) }, // 42
            new[] { (20, 22), (08, 21), (26, 04), (04, 02) }, // 43
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 44
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 45
            new[] { (21, 22), (09, 21), (26, 04), (07, 02) }, // 46
            new[] { (22, 22), (10, 21), (27, 03), (09, 01) }, // 47
            new[] { (23, 22), (11, 21), (26, 02), (12, 01) }, // 48
            new[] { (23, 22), (11, 21), (26, 03), (09, 02) }, // 49
            new[] { (23, 22), (11, 21), (26, 02), (12, 01) }, // 50
            new[] { (22, 22), (10, 21), (27, 03), (09, 01) }, // 51
            new[] { (21, 22), (09, 21), (26, 04), (07, 02) }, // 52
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 53
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 54
            new[] { (20, 22), (08, 21), (26, 04), (04, 02) }, // 55
            new[] { (19, 22), (07, 21), (25, 04), (03, 02) }, // 56
            new[] { (20, 22), (08, 21), (26, 04), (04, 02) }, // 57
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }, // 58
            new[] { (21, 22), (09, 21), (27, 04), (05, 02) }  // 59
        };
        #endregion

        private static readonly bool[,] _rightEye;
        private static readonly bool[,] _leftEye;
        private static readonly bool[,] _rightEar;
        private static readonly bool[,] _leftEar;

        static SpindaSpotRenderer()
        {
            static bool[,] Load(string resource)
            {
                uint spotColor = Renderer.RawColor(0, 255, 255, 255);

                Renderer.GetResourceBitmap(resource, out Size2D size, out uint[] bitmap);
                bool[,] arr = new bool[size.Height, size.Width];
                fixed (uint* src = bitmap)
                {
                    for (int y = 0; y < size.Height; y++)
                    {
                        for (int x = 0; x < size.Width; x++)
                        {
                            arr[y, x] = *Renderer.GetPixelAddress(src, size.Width, x, y) == spotColor;
                        }
                    }
                }
                return arr;
            }
            _rightEye = Load("Sprites.SpindaSpot_RightEye.png");
            _leftEye = Load("Sprites.SpindaSpot_LeftEye.png");
            _rightEar = Load("Sprites.SpindaSpot_RightEar.png");
            _leftEar = Load("Sprites.SpindaSpot_LeftEar.png");
        }

        private static (uint color, uint replacement)[] SpindaSpotColorsFromShininess(bool shiny)
        {
            uint color1 = Renderer.RawColor(230, 214, 165, 255);
            uint color1Replacement = shiny ? Renderer.RawColor(165, 206, 16, 255) : Renderer.RawColor(239, 82, 74, 255);
            uint color2 = Renderer.RawColor(206, 165, 115, 255);
            uint color2Replacement = shiny ? Renderer.RawColor(123, 156, 0, 255) : Renderer.RawColor(189, 74, 49, 255);
            return new[] { (color1, color1Replacement), (color2, color2Replacement) };
        }
        public static void Render(DecodedGIF img, uint pid, bool shiny)
        {
            (uint color, uint replacement)[] colors = SpindaSpotColorsFromShininess(shiny);

            for (int i = 0; i < img.Frames.Count; i++)
            {
                (int X, int Y)[] coords = _spindaSpotCoordinates[i];
                fixed (uint* dst = img.Frames[i].Bitmap)
                {
                    Render(dst, (uint)img.Width, (uint)img.Height, pid, coords, colors);
                }
            }
        }
        private static void Render(uint* dst, uint dstW, uint dstH, uint pid, (int X, int Y)[] coords, (uint color, uint replacement)[] colors)
        {
            byte b = (byte)((pid >> 24) & 0xFF);
            RenderSpot(dst, dstW, dstH, _rightEye, coords[0], b, colors);
            b = (byte)((pid >> 16) & 0xFF);
            RenderSpot(dst, dstW, dstH, _leftEye, coords[1], b, colors);
            b = (byte)((pid >> 8) & 0xFF);
            RenderSpot(dst, dstW, dstH, _rightEar, coords[2], b, colors);
            b = (byte)(pid & 0xFF);
            RenderSpot(dst, dstW, dstH, _leftEar, coords[3], b, colors);
        }

        private static void RenderSpot(uint* dst, uint dstW, uint dstH, bool[,] spot, (int X, int Y) center, byte data, (uint color, uint replacement)[] colors)
        {
            int spotX = data & 0xF;
            int spotY = (data >> 4) & 0xF;
            int height = spot.GetLength(0);
            int width = spot.GetLength(1);
            for (int py = 0; py < height; py++)
            {
                int bmpY = center.Y - 8 + spotY + py;
                if (bmpY < 0 || bmpY >= dstH)
                {
                    continue;
                }
                for (int px = 0; px < width; px++)
                {
                    int bmpX = center.X - 8 + spotX + px;
                    if (bmpX < 0 || bmpX >= dstW || !spot[py, px])
                    {
                        continue;
                    }
                    uint* dstPix = Renderer.GetPixelAddress(dst, dstW, bmpX, bmpY);
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
