using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Kermalis.PokemonGameEngine.Util
{
    internal static class Utils
    {
        private const string AssemblyPrefix = "Kermalis.PokemonGameEngine.Assets.";
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private static IPlatformRenderInterface _renderInterface = null;
        public static IPlatformRenderInterface RenderInterface
        {
            get
            {
                // This is done because the static constructor of Utils is called (by SetWorkingDirectory) before the Avalonia app is built
                if (_renderInterface == null)
                {
                    _renderInterface = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
                }
                return _renderInterface;
            }
        }
        public static Stream GetResourceStream(string resource)
        {
            return _assembly.GetManifestResourceStream(AssemblyPrefix + resource);
        }

        static Utils()
        {
            void Add(string resource, List<PBESpecies> list)
            {
                using (var reader = new StreamReader(GetResourceStream(resource)))
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
            //Add("PKMN.FemaleSpriteLookup.txt", _femaleSpriteLookup);
        }

        public static string WorkingDirectory { get; private set; }
        public static void SetWorkingDirectory(string workingDirectory)
        {
            PBEUtils.InitEngine(workingDirectory);
            WorkingDirectory = workingDirectory;
        }

        private static readonly object _femaleSpriteLookupLockObj = new object();
        private static readonly List<PBESpecies> _femaleMinispriteLookup = new List<PBESpecies>();
        private static bool HasFemaleSprite(PBESpecies species)
        {
            lock (_femaleSpriteLookupLockObj)
            {
                return _femaleMinispriteLookup.Contains(species);
            }
        }
        public static Bitmap GetMinispriteBitmap(PBESpecies species, PBEForm form, PBEGender gender, bool shiny)
        {
            string speciesStr = PBEDataUtils.GetNameOfForm(species, form) ?? species.ToString();
            string genderStr = gender == PBEGender.Female && HasFemaleSprite(species) ? "_F" : string.Empty;
            return new Bitmap(GetResourceStream("Pkmn.PKMN_" + speciesStr + (shiny ? "_S" : string.Empty) + genderStr + ".png"));
        }
    }
}
