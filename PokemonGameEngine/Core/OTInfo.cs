using System.Collections.Generic;

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
            int hashCode = 5741937;
            hashCode = hashCode * -1521134295 + TrainerID.GetHashCode();
            hashCode = hashCode * -1521134295 + SecretID.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TrainerName);
            hashCode = hashCode * -1521134295 + TrainerIsFemale.GetHashCode();
            hashCode = hashCode * -1521134295 + Language.GetHashCode();
            return hashCode;
        }
    }
}
