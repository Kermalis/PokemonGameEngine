using Kermalis.MapEditor.Core;
using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Kermalis.MapEditor.UI
{
    public sealed class EncounterModel : INotifyPropertyChanged
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
        private bool _avaloniaIssue4048__Dumb_AF_TBH = true; // https://github.com/AvaloniaUI/Avalonia/issues/4048
        public int Form
        {
            get => _avaloniaIssue4048__Dumb_AF_TBH ? -1 : (int)Encounter.Form;
            set
            {
                if (value == -1)
                {
                    _avaloniaIssue4048__Dumb_AF_TBH = true;
                    OnPropertyChanged(nameof(Form));
                }
                else // Doesn't matter if the value is the same, we need to update the UI
                {
                    _avaloniaIssue4048__Dumb_AF_TBH = false;
                    Encounter.Form = (PBEForm)value;
                    OnPropertyChanged(nameof(Form));
                }
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
                SelectableForms = forms.Select(f => PBELocalizedString.GetFormName(Encounter.Species, f).ToString()).ToArray();
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
    }
}
