using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Render;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Util
{
    internal sealed class SpriteUtils
    {
        private static readonly Sprite _substituteFrontSprite;
        private static readonly Sprite _substituteBackSprite;

        static SpriteUtils()
        {
            void Add(string resource, List<PBESpecies> list)
            {
                using (var reader = new StreamReader(Utils.GetResourceStream(resource)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!Enum.TryParse(line, out PBESpecies species))
                        {
                            throw new InvalidDataException($"Failed to parse \"{resource}\"");
                        }
                        list.Add(species);
                    }
                }
            }
            Add("Pkmn.FemaleMinispriteLookup.txt", _femaleMinispriteLookup);
            Add("Pkmn.FemaleSpriteLookup.txt", _femaleSpriteLookup);

            _substituteFrontSprite = Sprite.LoadOrGet("Pkmn.STATUS2_Substitute_F.gif");
            _substituteBackSprite = Sprite.LoadOrGet("Pkmn.STATUS2_Substitute_B.gif");
        }

        private static readonly object _femaleSpriteLookupLockObj = new object();
        private static readonly List<PBESpecies> _femaleMinispriteLookup = new List<PBESpecies>();
        private static readonly List<PBESpecies> _femaleSpriteLookup = new List<PBESpecies>();
        private static bool HasFemaleSprite(PBESpecies species, bool minisprite)
        {
            lock (_femaleSpriteLookupLockObj)
            {
                return (minisprite ? _femaleMinispriteLookup : _femaleSpriteLookup).Contains(species);
            }
        }
        public static Sprite GetMinisprite(PBESpecies species, PBEForm form, PBEGender gender, bool shiny)
        {
            string speciesStr = PBEDataUtils.GetNameOfForm(species, form) ?? species.ToString();
            string genderStr = gender == PBEGender.Female && HasFemaleSprite(species, true) ? "_F" : string.Empty;
            return Sprite.LoadOrGet("Pkmn.PKMN_" + speciesStr + (shiny ? "_S" : string.Empty) + genderStr + ".png");
        }
        public static Sprite GetPokemonSprite(PBESpecies species, PBEForm form, PBEGender gender, bool shiny, bool backSprite, bool behindSubstitute)
        {
            if (behindSubstitute)
            {
                return backSprite ? _substituteBackSprite : _substituteFrontSprite;
            }
            else
            {
                string speciesStr = PBEDataUtils.GetNameOfForm(species, form) ?? species.ToString();
                string genderStr = gender == PBEGender.Female && HasFemaleSprite(species, false) ? "_F" : string.Empty;
                string orientation = backSprite ? "_B" : "_F";
                return Sprite.LoadOrGet("Pkmn.PKMN_" + speciesStr + orientation + (shiny ? "_S" : string.Empty) + genderStr + ".gif");
            }
        }
    }
}
