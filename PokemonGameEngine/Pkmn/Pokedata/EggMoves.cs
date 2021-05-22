using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Util;
using System.Collections.Generic;
using System.Linq;

namespace Kermalis.PokemonGameEngine.Pkmn.Pokedata
{
    internal static class EggMoves
    {
        public static IEnumerable<PBEMove> GetEggMoves(PBESpecies species, PBEForm form)
        {
            PBEMove[] arr;

            string resource = "Pokedata." + Utils.GetPkmnDirectoryName(species, form) + ".EggMoves.bin";
            using (var r = new EndianBinaryReader(Utils.GetResourceStream(resource)))
            {
                arr = r.ReadEnums<PBEMove>(r.ReadByte());
            }

            return arr.Where(m => PBEDataUtils.IsMoveUsable(m)).Distinct(); // For now
        }
    }
}
