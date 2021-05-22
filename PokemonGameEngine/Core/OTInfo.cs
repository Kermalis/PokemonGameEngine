using System;

namespace Kermalis.PokemonGameEngine.Core
{
    internal enum Language : byte
    {
        Japanese,
        English,
        French,
        Italian,
        German,
        Spanish,
        Korean
    }

    internal sealed class OTInfo
    {
        public ushort TrainerID;
        public ushort SecretID; // Currently serves no purpose
        public string TrainerName;
        public bool TrainerIsFemale;
        public Language Language;

        // Debug
        public OTInfo(string name, bool female)
        {
            TrainerID = 1234;
            SecretID = 43210;
            TrainerName = name;
            TrainerIsFemale = female;
            Language = Language.English;
        }

        public override bool Equals(object obj)
        {
            return obj is OTInfo ot
                && ot.TrainerID == TrainerID
                && ot.SecretID == SecretID
                && ot.TrainerName == TrainerName
                && ot.TrainerIsFemale == TrainerIsFemale
                && ot.Language == Language;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(TrainerID, SecretID, TrainerName, TrainerIsFemale, Language);
        }
    }
}
