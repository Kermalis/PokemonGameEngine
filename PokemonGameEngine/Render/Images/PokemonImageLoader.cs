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
        private const string SUBSTITUTE_B_ASSET = "STATUS2_Substitute_B.gif";
        private const string SUBSTITUTE_F_ASSET = "STATUS2_Substitute_F.gif";
        private const string EGG_F_ASSET = "Egg_F.gif";
        private const string EGG_MINI_ASSET = "Egg_Mini.png";

        private static readonly List<PBESpecies> _femaleMiniLookup = new();
        private static readonly List<PBESpecies> _femaleVersionLookup = new();

        static PokemonImageLoader()
        {
            static void Add(string asset, List<PBESpecies> list)
            {
                using (StreamReader reader = File.OpenText(GetAssetPath(asset)))
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
            Add("FemaleMinispriteLookup.txt", _femaleMiniLookup);
            Add("FemaleSpriteLookup.txt", _femaleVersionLookup);
        }

        private static string GetAssetPath(string asset)
        {
            return AssetLoader.GetPath(asset, basePath: AssetLoader.PKMN_SPRITE_ASSET_PATH);
        }

        public static bool HasFemaleVersion(PBESpecies species, bool mini)
        {
            return (mini ? _femaleMiniLookup : _femaleVersionLookup).Contains(species);
        }

        public static AnimatedImage GetSubstituteImage(bool backImage)
        {
            return new AnimatedImage(GetAssetPath(backImage ? SUBSTITUTE_B_ASSET : SUBSTITUTE_F_ASSET));
        }
        public static AnimatedImage GetEggImage()
        {
            return new AnimatedImage(GetAssetPath(EGG_F_ASSET));
        }
        // Manaphy egg not considered but it can be
        public static Image GetEggMini()
        {
            return Image.LoadOrGet(GetAssetPath(EGG_MINI_ASSET));
        }

        public static Image GetMini(PBESpecies species, PBEForm form, PBEGender gender, bool shiny)
        {
            return Image.LoadOrGet(GetMiniAssetPath(species, form, gender, shiny));
        }
        public static string GetMiniAssetPath(PBESpecies species, PBEForm form, PBEGender gender, bool shiny)
        {
            StringBuilder sb = StartAssetString();
            AppendSpeciesPart(species, form, sb);
            AppendShinyPart(shiny, sb);
            AppendGenderPart(species, gender, true, sb);
            sb.Append(".png");
            return GetAssetPath(sb.ToString());
        }

        public static AnimatedImage GetPokemonImage(PBESpecies species, PBEForm form, PBEGender gender, bool shiny, uint pid, bool backImage)
        {
            bool spindaSpots = species == PBESpecies.Spinda && !backImage;
            return new AnimatedImage(GetPokemonImageAssetPath(species, form, gender, shiny, backImage), spindaSpots ? (pid, shiny) : null);
        }
        public static string GetPokemonImageAssetPath(PBESpecies species, PBEForm form, PBEGender gender, bool shiny, bool backImage)
        {
            StringBuilder sb = StartAssetString();
            AppendSpeciesPart(species, form, sb);
            sb.Append(backImage ? "_B" : "_F");
            AppendShinyPart(shiny, sb);
            AppendGenderPart(species, gender, false, sb);
            sb.Append(".gif");
            return GetAssetPath(sb.ToString());
        }

        private static StringBuilder StartAssetString()
        {
            return new StringBuilder("PKMN_");
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
