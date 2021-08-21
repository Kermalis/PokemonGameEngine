namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        public static void ModulateRectangle(uint* dst, uint dstW, uint dstH, float rMod, float gMod, float bMod, float aMod)
        {
            for (int y = 0; y < dstH; y++)
            {
                for (int x = 0; x < dstW; x++)
                {
                    ModulatePoint_Unchecked(GetPixelAddress(dst, dstW, x, y), rMod, gMod, bMod, aMod);
                }
            }
        }
    }
}
