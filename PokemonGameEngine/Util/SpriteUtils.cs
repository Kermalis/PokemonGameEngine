using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Render;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Util
{
    internal sealed class SpriteUtils
    {
        public const string SubstituteFrontSpriteResource = "Pkmn.STATUS2_Substitute_F.gif";
        public const string SubstituteBackSpriteResource = "Pkmn.STATUS2_Substitute_B.gif";

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
        }

        private static readonly object _femaleSpriteLookupLockObj = new object();
        private static readonly List<PBESpecies> _femaleMinispriteLookup = new List<PBESpecies>();
        private static readonly List<PBESpecies> _femaleSpriteLookup = new List<PBESpecies>();
        public static bool HasFemaleSprite(PBESpecies species, bool minisprite)
        {
            lock (_femaleSpriteLookupLockObj)
            {
                return (minisprite ? _femaleMinispriteLookup : _femaleSpriteLookup).Contains(species);
            }
        }
        public static Sprite GetMinisprite(PBESpecies species, PBEForm form, PBEGender gender, bool shiny)
        {
            return Sprite.LoadOrGet(GetMinispriteResource(species, form, gender, shiny));
        }
        public static string GetMinispriteResource(PBESpecies species, PBEForm form, PBEGender gender, bool shiny)
        {
            string speciesStr = PBEDataUtils.GetNameOfForm(species, form) ?? species.ToString();
            string genderStr = gender == PBEGender.Female && HasFemaleSprite(species, true) ? "_F" : string.Empty;
            return "Pkmn.PKMN_" + speciesStr + (shiny ? "_S" : string.Empty) + genderStr + ".png";
        }
        public static Sprite GetPokemonSprite(PBESpecies species, PBEForm form, PBEGender gender, bool shiny, bool backSprite, bool behindSubstitute)
        {
            return Sprite.LoadOrGet(GetPokemonSpriteResource(species, form, gender, shiny, backSprite, behindSubstitute));
        }
        public static string GetPokemonSpriteResource(PBESpecies species, PBEForm form, PBEGender gender, bool shiny, bool backSprite, bool behindSubstitute)
        {
            if (behindSubstitute)
            {
                return backSprite ? SubstituteBackSpriteResource : SubstituteFrontSpriteResource;
            }
            else
            {
                string speciesStr = PBEDataUtils.GetNameOfForm(species, form) ?? species.ToString();
                string genderStr = gender == PBEGender.Female && HasFemaleSprite(species, false) ? "_F" : string.Empty;
                string orientation = backSprite ? "_B" : "_F";
                return "Pkmn.PKMN_" + speciesStr + orientation + (shiny ? "_S" : string.Empty) + genderStr + ".gif";
            }
        }
    }
}
