using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Render;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Kermalis.PokemonGameEngine.Core
{
    internal static class AssetLoader
    {
        private const string AssetPath = "Assets";

        public static void InitBattleEngine()
        {
            _ = new BattleEngineDataProvider(AssetPath);
        }

        public static string GetPath(string asset)
        {
            asset = Path.Combine(AssetPath, asset);
            if (!File.Exists(asset))
            {
                throw new ArgumentOutOfRangeException(nameof(asset), "Asset not found: " + asset);
            }
            return asset;
        }
        public static FileStream GetAssetStream(string asset)
        {
            return File.OpenRead(GetPath(asset));
        }
        public static StreamReader GetAssetStreamText(string asset)
        {
            return File.OpenText(GetPath(asset));
        }

        public static string GetPkmnDirectoryName(PBESpecies species, PBEForm form)
        {
            return form == 0 ? species.ToString() : PBEDataUtils.GetNameOfForm(species, form);
        }

        public static unsafe void GetAssetBitmap(string asset, out Size2D size, out uint[] dstBmp)
        {
            using (FileStream s = GetAssetStream(asset))
            using (var img = SixLabors.ImageSharp.Image.Load<Rgba32>(s))
            {
                size.Width = (uint)img.Width;
                size.Height = (uint)img.Height;
                dstBmp = new uint[size.GetArea()];
                fixed (uint* dst = dstBmp)
                {
                    uint len = size.GetArea() * sizeof(uint);
                    fixed (void* data = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0)))
                    {
                        Buffer.MemoryCopy(data, dst, len, len);
                    }
                }
            }
        }
    }
}
