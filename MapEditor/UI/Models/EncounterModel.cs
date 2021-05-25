using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Kermalis.MapEditor.UI.Models
{
    public sealed class EncounterModel : INotifyPropertyChanged, IDisposable
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler RemoveReady;

        public byte Chance
        {
            get => Encounter.Chance;
            set
            {
                if (value != Encounter.Chance)
                {
                    Encounter.Chance = value;
                    OnPropertyChanged(nameof(Chance));
                }
            }
        }
        public string ChanceProbability { get; private set; }
        public string CombinedProbability { get; private set; }
        public byte MinLevel
        {
            get => Encounter.MinLevel;
            set
            {
                if (value != Encounter.MinLevel)
                {
                    Encounter.MinLevel = value;
                    OnPropertyChanged(nameof(MinLevel));
                }
            }
        }
        public byte MaxLevel
        {
            get => Encounter.MaxLevel;
            set
            {
                if (value != Encounter.MaxLevel)
                {
                    Encounter.MaxLevel = value;
                    OnPropertyChanged(nameof(MaxLevel));
                }
            }
        }
        public PBESpecies Species
        {
            get => Encounter.Species;
            set
            {
                if (value != Encounter.Species)
                {
                    Encounter.Species = value;
                    OnPropertyChanged(nameof(Species));
                    UpdateForms(0);
                }
            }
        }
        public int Form
        {
            get => (int)Encounter.Form;
            set
            {
                // -1 is set but it's always followed by 0.
                if (value == -1)
                {
                    return;
                }
                Encounter.Form = (PBEForm)value;
                OnPropertyChanged(nameof(Form));
            }
        }
        public IEnumerable<string> SelectableForms { get; private set; }
        public bool FormsEnabled { get; private set; }

        internal readonly EncounterTable.Encounter Encounter;

        internal EncounterModel(EncounterTable.Encounter e)
        {
            Encounter = e;
            UpdateForms(e.Form);
        }

        private static readonly string[] _baseForm = new string[1] { "Base" };
        private void UpdateForms(PBEForm form)
        {
            IReadOnlyList<PBEForm> forms = PBEDataUtils.GetForms(Encounter.Species, false);
            if (forms.Count == 0)
            {
                SelectableForms = _baseForm;
                FormsEnabled = false;
            }
            else
            {
                // Order by form id so the correct form index is chosen by UI
                SelectableForms = Utils.GetOrderedFormStrings(forms, Encounter.Species);
                FormsEnabled = true;
            }
            OnPropertyChanged(nameof(SelectableForms));
            Form = (int)form;
            OnPropertyChanged(nameof(FormsEnabled));
        }

        internal void UpdateChanceProbability(float perStep, ushort combined)
        {
            float p = combined == 0 ? 0f : (float)Encounter.Chance / combined;
            ChanceProbability = "Probability per encounter: " + p.ToString("P2");
            OnPropertyChanged(nameof(ChanceProbability));
            CombinedProbability = "Combined probability: " + (p * perStep).ToString("P2");
            OnPropertyChanged(nameof(CombinedProbability));
        }

        public void Remove()
        {
            RemoveReady?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            PropertyChanged = null;
            RemoveReady = null;
        }
    }
}
