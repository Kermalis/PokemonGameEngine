using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    /// <summary>Helps with writing to pixel arrays. Colors are RGBA8888 (0xAABBCCDD - AA is Alpha, BB is Blue, CC is Green, DD is Red)</summary>
    internal static unsafe class UnsafeRenderer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RawColor(uint r, uint g, uint b, uint a)
        {
            return (a << 24) | (b << 16) | (g << 8) | r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPixelIndex(int imgW, Vec2I pos)
        {
            return pos.X + (pos.Y * imgW);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint* GetPixelAddress(uint* img, int imgW, Vec2I pos)
        {
            return img + GetPixelIndex(imgW, pos);
        }
    }
}
