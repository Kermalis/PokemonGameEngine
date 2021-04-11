using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Util;

namespace Kermalis.PokemonGameEngine.Pkmn.Pokedata
{
    internal sealed class EvolutionData
    {
        public sealed class EvoData
        {
            public EvoMethod Method { get; }
            public ushort Param { get; }
            public PBESpecies Species { get; }
            public PBEForm Form { get; }

            public EvoData(EndianBinaryReader r)
            {
                Method = r.ReadEnum<EvoMethod>();
                Param = r.ReadUInt16();
                Species = r.ReadEnum<PBESpecies>();
                Form = r.ReadEnum<PBEForm>();
            }
        }

        public PBESpecies BabySpecies { get; }
        public EvoData[] Evolutions { get; }

        public EvolutionData(PBESpecies species, PBEForm form)
        {
            string resource = "Pokedata." + PkmnOrderResolver.GetDirectoryName(species, form) + ".Evolutions.bin";
            using (var r = new EndianBinaryReader(Utils.GetResourceStream(resource)))
            {
                BabySpecies = r.ReadEnum<PBESpecies>();
                byte count = r.ReadByte();
                Evolutions = new EvoData[count];
                for (int i = 0; i < count; i++)
                {
                    Evolutions[i] = new EvoData(r);
                }
            }
        }

        public bool IsSpeciesFutureEvo(PBESpecies species)
        {
            foreach (EvoData e in Evolutions)
            {
                if (e.Species == species)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
