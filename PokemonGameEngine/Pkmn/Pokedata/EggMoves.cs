using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kermalis.PokemonGameEngine.Pkmn.Pokedata
{
    internal static class EggMoves
    {
        public static IEnumerable<PBEMove> GetEggMoves(PBESpecies species, PBEForm form)
        {
            PBEMove[] arr;

            string asset = "Pokedata\\" + AssetLoader.GetPkmnDirectoryName(species, form) + "\\EggMoves.bin";
            using (var r = new EndianBinaryReader(AssetLoader.GetAssetStream(asset)))
            {
                arr = r.ReadEnums<PBEMove>(r.ReadByte());
            }

            return Array.FindAll(arr, m => PBEDataUtils.IsMoveUsable(m)).Distinct(); // For now
        }
    }
}
