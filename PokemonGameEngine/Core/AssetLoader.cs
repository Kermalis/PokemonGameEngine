using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using System;
using System.IO;

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
    }
}
