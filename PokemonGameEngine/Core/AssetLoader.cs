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
        public const string DEFAULT_ASSET_PATH =
#if DEBUG
            @"..\..\..\Assets";
#else
            @"Assets";
#endif
        public const string PKMN_SPRITE_ASSET_PATH =
#if DEBUG
            @"..\..\..\..\..\PokemonBattleEngine\Shared Assets\PKMN";
#else
            @"Assets\Pkmn";
#endif
        private const string PBE_ASSET_PATH =
#if DEBUG
            @"..\..\..\..\Shared Dependencies";
#else
            @"Assets";
#endif

        public static void InitBattleEngineProvider()
        {
            _ = new BattleEngineDataProvider(PBE_ASSET_PATH);
        }

        public static string GetPath(string asset, string basePath = DEFAULT_ASSET_PATH)
        {
            asset = Path.Combine(basePath, asset);
            if (!File.Exists(asset))
            {
                throw new ArgumentOutOfRangeException(nameof(asset), "Asset not found: " + asset);
            }
            return asset;
        }

        public static string GetPkmnDirectoryName(PBESpecies species, PBEForm form)
        {
            return form == 0 ? species.ToString() : PBEDataUtils.GetNameOfForm(species, form);
        }

        public static unsafe uint[] GetAssetBitmap(string assetPath, out Vec2I size)
        {
            using (FileStream s = File.OpenRead(assetPath))
            using (var img = SixLabors.ImageSharp.Image.Load<Rgba32>(s))
            {
                size.X = img.Width;
                size.Y = img.Height;
                uint[] dstBmp = new uint[size.GetArea()];
                fixed (uint* dst = dstBmp)
                {
                    uint len = (uint)dstBmp.Length * sizeof(uint);
                    fixed (void* data = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0)))
                    {
                        Buffer.MemoryCopy(data, dst, len, len);
                    }
                }
                return dstBmp;
            }
        }
    }
}
