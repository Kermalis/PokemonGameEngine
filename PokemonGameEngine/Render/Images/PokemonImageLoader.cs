using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kermalis.PokemonGameEngine.Render.Images
{
    internal sealed class PokemonImageLoader
    {
        private const string SUBSTITUTE_B_ASSET = @"Pkmn\STATUS2_Substitute_B.gif";
        private const string SUBSTITUTE_F_ASSET = @"Pkmn\STATUS2_Substitute_F.gif";
        private const string EGG_F_ASSET = @"Pkmn\Egg_F.gif";
        private const string EGG_MINI_ASSET = @"Pkmn\Egg_Mini.png";

        private static readonly List<PBESpecies> _femaleMiniLookup = new();
        private static readonly List<PBESpecies> _femaleVersionLookup = new();

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
            Add(@"Pkmn\FemaleMinispriteLookup.txt", _femaleMiniLookup);
            Add(@"Pkmn\FemaleSpriteLookup.txt", _femaleVersionLookup);
        }

        public static bool HasFemaleVersion(PBESpecies species, bool mini)
        {
            return (mini ? _femaleMiniLookup : _femaleVersionLookup).Contains(species);
        }

        public static AnimatedImage GetSubstituteImage(bool backImage)
        {
            return new AnimatedImage(backImage ? SUBSTITUTE_B_ASSET : SUBSTITUTE_F_ASSET);
        }
        public static AnimatedImage GetEggImage()
        {
            return new AnimatedImage(EGG_F_ASSET);
        }
        // Manaphy egg not considered but it can be
        public static Image GetEggMini()
        {
            return Image.LoadOrGet(EGG_MINI_ASSET);
        }

        public static Image GetMini(PBESpecies species, PBEForm form, PBEGender gender, bool shiny)
        {
            return Image.LoadOrGet(GetMiniAsset(species, form, gender, shiny));
        }
        public static string GetMiniAsset(PBESpecies species, PBEForm form, PBEGender gender, bool shiny)
        {
            StringBuilder sb = StartAssetString();
            AppendSpeciesPart(species, form, sb);
            AppendShinyPart(shiny, sb);
            AppendGenderPart(species, gender, true, sb);
            sb.Append(".png");
            return sb.ToString();
        }

        public static AnimatedImage GetPokemonImage(PBESpecies species, PBEForm form, PBEGender gender, bool shiny, uint pid, bool backImage)
        {
            bool spindaSpots = species == PBESpecies.Spinda && !backImage;
            return new AnimatedImage(GetPokemonImageAsset(species, form, gender, shiny, backImage), spindaSpots ? (pid, shiny) : null);
        }
        public static string GetPokemonImageAsset(PBESpecies species, PBEForm form, PBEGender gender, bool shiny, bool backImage)
        {
            StringBuilder sb = StartAssetString();
            AppendSpeciesPart(species, form, sb);
            sb.Append(backImage ? "_B" : "_F");
            AppendShinyPart(shiny, sb);
            AppendGenderPart(species, gender, false, sb);
            sb.Append(".gif");
            return sb.ToString();
        }

        private static StringBuilder StartAssetString()
        {
            return new StringBuilder(@"Pkmn\PKMN_");
        }
        private static void AppendSpeciesPart(PBESpecies species, PBEForm form, StringBuilder sb)
        {
            sb.Append(PBEDataUtils.GetNameOfForm(species, form) ?? species.ToString());
        }
        private static void AppendShinyPart(bool shiny, StringBuilder sb)
        {
            if (shiny)
            {
                sb.Append("_S");
            }
        }
        private static void AppendGenderPart(PBESpecies species, PBEGender gender, bool mini, StringBuilder sb)
        {
            if (gender == PBEGender.Female && HasFemaleVersion(species, mini))
            {
                sb.Append("_F");
            }
        }
    }
}
