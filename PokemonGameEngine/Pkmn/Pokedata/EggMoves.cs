using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Util;
using System.Linq;

namespace Kermalis.PokemonGameEngine.Pkmn.Pokedata
{
    internal static class EggMoves
    {
        public static PBEMove[] GetEggMoves(PBESpecies species, PBEForm form)
        {
            PBEMove[] ret;

            string resource = "Pokedata." + PkmnOrderResolver.GetDirectoryName(species, form) + ".EggMoves.bin";
            using (var r = new EndianBinaryReader(Utils.GetResourceStream(resource)))
            {
                ret = r.ReadEnums<PBEMove>(r.ReadByte());
            }

            return ret.Where(m => PBEDataUtils.IsMoveUsable(m)).Distinct().ToArray(); // For now
        }
    }
}
