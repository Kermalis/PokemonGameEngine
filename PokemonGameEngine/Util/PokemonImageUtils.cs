using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Render;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Util
{
    internal sealed class PokemonImageUtils
    {
        public const string SubstituteFrontResource = "Pkmn.STATUS2_Substitute_F.gif";
        public const string SubstituteBackResource = "Pkmn.STATUS2_Substitute_B.gif";

        static PokemonImageUtils()
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
            Add("Pkmn.FemaleMinispriteLookup.txt", _femaleMiniLookup);
            Add("Pkmn.FemaleSpriteLookup.txt", _femaleVersionLookup);
        }

        private static readonly object _femaleLookupLockObj = new object();
        private static readonly List<PBESpecies> _femaleMiniLookup = new List<PBESpecies>();
        private static readonly List<PBESpecies> _femaleVersionLookup = new List<PBESpecies>();
        public static bool HasFemaleVersion(PBESpecies species, bool mini)
        {
            lock (_femaleLookupLockObj)
            {
                return (mini ? _femaleMiniLookup : _femaleVersionLookup).Contains(species);
            }
        }
        public static Image GetMini(PBESpecies species, PBEForm form, PBEGender gender, bool shiny)
        {
            return Image.LoadOrGet(GetMiniResource(species, form, gender, shiny));
        }
        public static string GetMiniResource(PBESpecies species, PBEForm form, PBEGender gender, bool shiny)
        {
            string speciesStr = PBEDataUtils.GetNameOfForm(species, form) ?? species.ToString();
            string genderStr = gender == PBEGender.Female && HasFemaleVersion(species, true) ? "_F" : string.Empty;
            return "Pkmn.PKMN_" + speciesStr + (shiny ? "_S" : string.Empty) + genderStr + ".png";
        }
        public static AnimatedImage GetPokemonImage(PBESpecies species, PBEForm form, PBEGender gender, bool shiny, bool backImage, bool behindSubstitute, uint pid)
        {
            bool doSpindaSpots = species == PBESpecies.Spinda && !backImage && !behindSubstitute;
            var ret = new AnimatedImage(GetPokemonImageResource(species, form, gender, shiny, backImage, behindSubstitute), !doSpindaSpots);
            if (doSpindaSpots)
            {
                RenderUtils.RenderSpindaSpots(ret, pid, shiny);
            }
            return ret;
        }
        public static string GetPokemonImageResource(PBESpecies species, PBEForm form, PBEGender gender, bool shiny, bool backImage, bool behindSubstitute)
        {
            if (behindSubstitute)
            {
                return backImage ? SubstituteBackResource : SubstituteFrontResource;
            }
            else
            {
                string speciesStr = PBEDataUtils.GetNameOfForm(species, form) ?? species.ToString();
                string genderStr = gender == PBEGender.Female && HasFemaleVersion(species, false) ? "_F" : string.Empty;
                string orientation = backImage ? "_B" : "_F";
                return "Pkmn.PKMN_" + speciesStr + orientation + (shiny ? "_S" : string.Empty) + genderStr + ".gif";
            }
        }
    }
}
