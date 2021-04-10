using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal static class PkmnOrderResolver
    {
        private static readonly Dictionary<PBESpecies, int> _formTable = new Dictionary<PBESpecies, int>
        {
            { PBESpecies.Unown, 28 },
            { PBESpecies.Castform, 4 },
            { PBESpecies.Deoxys, 4 },
            { PBESpecies.Burmy, 3 },
            { PBESpecies.Wormadam, 3 },
            { PBESpecies.Cherrim, 2 },
            { PBESpecies.Shellos, 2 },
            { PBESpecies.Gastrodon, 2 },
            { PBESpecies.Rotom, 5 },
            { PBESpecies.Giratina, 2 },
            { PBESpecies.Shaymin, 2 },
            { PBESpecies.Arceus, 17 },
            { PBESpecies.Basculin, 2 },
            { PBESpecies.Darmanitan, 2 },
            { PBESpecies.Deerling, 4 },
            { PBESpecies.Sawsbuck, 4 },
            { PBESpecies.Tornadus, 2 },
            { PBESpecies.Thundurus, 2 },
            { PBESpecies.Landorus, 2 },
            { PBESpecies.Kyurem, 3 },
            { PBESpecies.Keldeo, 2 },
            { PBESpecies.Meloetta, 2 },
            { PBESpecies.Genesect, 5 }
        };

        public static string GetDirectoryName(PBESpecies species, PBEForm form)
        {
            string dir;
            if (form == 0)
            {
                dir = species.ToString();
            }
            else
            {
                dir = PBEDataUtils.GetNameOfForm(species, form);
            }
            return dir;
        }

        public static int GetIndexForBinary(PBESpecies species, PBEForm form)
        {
            const int maxSpecies = 649;
            if (species == 0 || species >= (PBESpecies)maxSpecies)
            {
                throw new ArgumentOutOfRangeException(nameof(species));
            }
            if (form == 0)
            {
                return (int)species;
            }

            int index = maxSpecies;
            foreach (KeyValuePair<PBESpecies, int> kvp in _formTable)
            {
                PBESpecies fSpecies = kvp.Key;
                int fNumForms = kvp.Value;
                if (fSpecies < species)
                {
                    index += fNumForms - 1;
                }
                else if (fSpecies == species)
                {
                    if ((int)form >= fNumForms)
                    {
                        throw new ArgumentOutOfRangeException(nameof(form));
                    }
                    return index + (int)form;
                }
            }
            // Could not find form in form table
            throw new ArgumentOutOfRangeException(nameof(form));
        }
    }
}
