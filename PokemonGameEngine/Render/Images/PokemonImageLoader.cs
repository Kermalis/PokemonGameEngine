using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Render.Images
{
    internal sealed class PokemonImageLoader
    {
        public const string SubstituteFrontAsset = "Pkmn\\STATUS2_Substitute_F.gif";
        public const string SubstituteBackAsset = "Pkmn\\STATUS2_Substitute_B.gif";
        public const string EggFrontAsset = "Pkmn\\Egg_F.gif";
        public const string EggBackAsset = "Pkmn\\Egg_B.gif";
        public const string EggMiniAsset = "Pkmn\\Egg_Mini.png";

        static PokemonImageLoader()
        {
            static void Add(string asset, List<PBESpecies> list)
            {
                using (StreamReader reader = AssetLoader.GetAssetStreamText(asset))
                {
                    string line;
                    while ((line = reader.ReadLine()) is not null)
                    {
                        if (!Enum.TryParse(line, out PBESpecies species))
                        {
                            throw new InvalidDataException($"Failed to parse \"{asset}\"");
                        }
                        list.Add(species);
                    }
                }
            }
            Add("Pkmn\\FemaleMinispriteLookup.txt", _femaleMiniLookup);
            Add("Pkmn\\FemaleSpriteLookup.txt", _femaleVersionLookup);
        }

        private static readonly object _femaleLookupLockObj = new();
        private static readonly List<PBESpecies> _femaleMiniLookup = new();
        private static readonly List<PBESpecies> _femaleVersionLookup = new();
        public static bool HasFemaleVersion(PBESpecies species, bool mini)
        {
            lock (_femaleLookupLockObj)
            {
                return (mini ? _femaleMiniLookup : _femaleVersionLookup).Contains(species);
            }
        }
        public static Image GetMini(PBESpecies species, PBEForm form, PBEGender gender, bool shiny, bool isEgg)
        {
            return Image.LoadOrGet(GetMiniAsset(species, form, gender, shiny, isEgg));
        }
        public static string GetMiniAsset(PBESpecies species, PBEForm form, PBEGender gender, bool shiny, bool isEgg)
        {
            if (isEgg)
            {
                return EggMiniAsset;
            }
            string speciesStr = PBEDataUtils.GetNameOfForm(species, form) ?? species.ToString();
            string genderStr = gender == PBEGender.Female && HasFemaleVersion(species, true) ? "_F" : string.Empty;
            return "Pkmn\\PKMN_" + speciesStr + (shiny ? "_S" : string.Empty) + genderStr + ".png";
        }
        public static AnimatedImage GetPokemonImage(PBESpecies species, PBEForm form, PBEGender gender, bool shiny, bool backImage, bool behindSubstitute, uint pid, bool isEgg)
        {
            bool doSpindaSpots = !isEgg && species == PBESpecies.Spinda && !backImage && !behindSubstitute;
            return new AnimatedImage(GetPokemonImageAsset(species, form, gender, shiny, backImage, behindSubstitute, isEgg), doSpindaSpots ? (pid, shiny) : null);
        }
        public static string GetPokemonImageAsset(PBESpecies species, PBEForm form, PBEGender gender, bool shiny, bool backImage, bool behindSubstitute, bool isEgg)
        {
            if (behindSubstitute)
            {
                return backImage ? SubstituteBackAsset : SubstituteFrontAsset;
            }
            if (isEgg)
            {
                return backImage ? EggBackAsset : EggFrontAsset;
            }

            string speciesStr = PBEDataUtils.GetNameOfForm(species, form) ?? species.ToString();
            string genderStr = gender == PBEGender.Female && HasFemaleVersion(species, false) ? "_F" : string.Empty;
            string orientation = backImage ? "_B" : "_F";
            return "Pkmn\\PKMN_" + speciesStr + orientation + (shiny ? "_S" : string.Empty) + genderStr + ".gif";
        }
    }
}
