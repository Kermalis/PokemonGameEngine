using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class OTInfo
    {
        public ushort TrainerID;
        public ushort SecretID;
        public string TrainerName;
        public bool TrainerIsFemale;

        public OTInfo(string name, bool female)
        {
            TrainerID = 1234;
            SecretID = 43210;
            TrainerName = name;
            TrainerIsFemale = female;
        }

        public override bool Equals(object obj)
        {
            return obj is OTInfo ot
                && ot.TrainerID == TrainerID
                && ot.SecretID == SecretID
                && ot.TrainerName == TrainerName
                && ot.TrainerIsFemale == TrainerIsFemale;
        }
        public override int GetHashCode()
        {
            int hashCode = 692070374;
            hashCode = hashCode * -1521134295 + TrainerID.GetHashCode();
            hashCode = hashCode * -1521134295 + SecretID.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TrainerName);
            hashCode = hashCode * -1521134295 + TrainerIsFemale.GetHashCode();
            return hashCode;
        }
    }
}
