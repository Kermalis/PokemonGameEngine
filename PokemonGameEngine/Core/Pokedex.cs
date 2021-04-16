using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Util;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class Pokedex
    {
        [DebuggerDisplay("Seen={" + nameof(Seen) + "}, Caught={" + nameof(Caught) + "}")]
        private sealed class Entry
        {
            public readonly PBESpecies Species;
            public readonly PBEForm Form;
            public readonly PBEGender Gender;

            public bool Seen;
            public bool Caught;

            public Entry(PBESpecies species, PBEForm form, PBEGender gender)
            {
                Species = species;
                Form = form;
                Gender = gender;
            }
        }

        private readonly Dictionary<PBESpecies, Dictionary<PBEForm, Dictionary<PBEGender, Entry>>> _data;
        public uint SpindaPID;

        public Pokedex()
        {
            _data = new Dictionary<PBESpecies, Dictionary<PBEForm, Dictionary<PBEGender, Entry>>>((int)PBESpecies.MAX);
            for (var species = (PBESpecies)1; species < PBESpecies.MAX; species++)
            {
                PBEGender[] genders = GetGenderKeys(species);
                IReadOnlyList<PBEForm> forms = PBEDataUtils.GetForms(species, false);
                if (forms.Count == 0)
                {
                    forms = new PBEForm[] { 0 };
                }
                var formDict = new Dictionary<PBEForm, Dictionary<PBEGender, Entry>>(forms.Count);
                for (int f = 0; f < forms.Count; f++)
                {
                    PBEForm form = forms[f];
                    var genderDict = new Dictionary<PBEGender, Entry>(genders.Length);
                    for (int g = 0; g < genders.Length; g++)
                    {
                        PBEGender gender = genders[g];
                        genderDict.Add(gender, new Entry(species, form, gender));
                    }
                    formDict.Add(form, genderDict);
                }
                _data.Add(species, formDict);
            }
        }

        private PBEGender GetGenderKey(PBESpecies species, PBEGender gender)
        {
            return PokemonImageUtils.HasFemaleVersion(species, false) ? gender : PBEGender.MAX;
        }
        private PBEGender[] GetGenderKeys(PBESpecies species)
        {
            if (PokemonImageUtils.HasFemaleVersion(species, false))
            {
                return new[] { PBEGender.Male, PBEGender.Female };
            }
            return new[] { PBEGender.MAX };
        }

        private Entry GetFirstOrNullSeenEntry(PBESpecies species)
        {
            foreach (KeyValuePair<PBEForm, Dictionary<PBEGender, Entry>> forms in _data[species])
            {
                foreach (KeyValuePair<PBEGender, Entry> genders in forms.Value)
                {
                    Entry en = genders.Value;
                    if (en.Seen)
                    {
                        return en;
                    }
                }
            }
            return null;
        }
        private Entry GetFirstOrNullCaughtEntry(PBESpecies species)
        {
            foreach (KeyValuePair<PBEForm, Dictionary<PBEGender, Entry>> forms in _data[species])
            {
                foreach (KeyValuePair<PBEGender, Entry> genders in forms.Value)
                {
                    Entry en = genders.Value;
                    if (en.Caught)
                    {
                        return en;
                    }
                }
            }
            return null;
        }

        public bool IsSeen(PBESpecies species)
        {
            return GetFirstOrNullSeenEntry(species) != null;
        }
        public bool IsCaught(PBESpecies species)
        {
            return GetFirstOrNullCaughtEntry(species) != null;
        }

        public int GetSpeciesSeen()
        {
            return _data.Count(p => IsSeen(p.Key));
        }
        public int GetSpeciesCaught()
        {
            return _data.Count(p => IsCaught(p.Key));
        }

        private void SetSpindaPIDIfFirstSpinda(Entry en, uint pid)
        {
            if (!en.Seen && en.Species == PBESpecies.Spinda)
            {
                SpindaPID = pid;
            }
        }
        public void SetSeen(PBESpecies species, PBEForm form, PBEGender gender, uint pid)
        {
            gender = GetGenderKey(species, gender);
            Entry en = _data[species][form][gender];
            SetSpindaPIDIfFirstSpinda(en, pid);
            en.Seen = true;
        }
        public void SetCaught(PBESpecies species, PBEForm form, PBEGender gender, uint pid)
        {
            gender = GetGenderKey(species, gender);
            Entry en = _data[species][form][gender];
            SetSpindaPIDIfFirstSpinda(en, pid);
            en.Seen = true;
            en.Caught = true;
        }
    }
}
