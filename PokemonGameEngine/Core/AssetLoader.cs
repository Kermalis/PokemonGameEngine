using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Images;
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
                dstBmp = new uint[size.Width * size.Height];
                fixed (uint* dst = dstBmp)
                {
                    uint len = size.Width * size.Height * sizeof(uint);
                    fixed (void* data = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0)))
                    {
                        Buffer.MemoryCopy(data, dst, len, len);
                    }
                }
            }
        }
        public static Image[] GetAssetSheetAsImages(string asset, Size2D imageSize)
        {
            uint[][] bitmaps = GetAssetSheetAsBitmaps(asset, imageSize);
            var arr = new Image[bitmaps.Length];
            for (int i = 0; i < bitmaps.Length; i++)
            {
                arr[i] = new Image(bitmaps[i], imageSize, asset + '[' + i + ']');
            }
            return arr;
        }
        public static unsafe uint[][] GetAssetSheetAsBitmaps(string asset, Size2D imageSize)
        {
            GetAssetBitmap(asset, out Size2D sheetSize, out uint[] srcBmp);
            fixed (uint* src = srcBmp)
            {
                uint numImagesX = sheetSize.Width / imageSize.Width;
                uint numImagesY = sheetSize.Height / imageSize.Height;
                uint[][] imgs = new uint[numImagesX * numImagesY][];
                int img = 0;
                for (uint sy = 0; sy < numImagesY; sy++)
                {
                    for (uint sx = 0; sx < numImagesX; sx++)
                    {
                        imgs[img++] = Renderer.GetBitmap_Unchecked(src, sheetSize.Width, new Pos2D((int)(sx * imageSize.Width), (int)(sy * imageSize.Height)), imageSize);
                    }
                }
                return imgs;
            }
        }
    }
}
