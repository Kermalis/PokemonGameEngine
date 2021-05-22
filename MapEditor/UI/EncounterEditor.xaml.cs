using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.UI.Models;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Kermalis.MapEditor.UI
{
    public sealed class EncounterEditor : UserControl, INotifyPropertyChanged, IDisposable
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        public byte Chance
        {
            get => _tbl is null ? byte.MinValue : _tbl.ChanceOfPhenomenon;
            set
            {
                if (value != _tbl.ChanceOfPhenomenon)
                {
                    _tbl.ChanceOfPhenomenon = value;
                    OnPropertyChanged(nameof(Chance));
                    UpdateChanceProbabilityText();
                    UpdateEncounterChanceProbabilities();
                }
            }
        }
        public string ChanceProbability { get; private set; }
        private bool _addEncounterEnabled;
        public bool AddEncounterEnabled
        {
            get => _addEncounterEnabled;
            private set
            {
                if (_addEncounterEnabled != value)
                {
                    _addEncounterEnabled = value;
                    OnPropertyChanged(nameof(AddEncounterEnabled));
                }
            }
        }
        private string _numEncountersText;
        public string NumEncountersText
        {
            get => _numEncountersText;
            private set
            {
                if (value != _numEncountersText)
                {
                    _numEncountersText = value;
                    OnPropertyChanged(nameof(NumEncountersText));
                }
            }
        }

        private bool _tableExists;
        public bool TableExists
        {
            get => _tableExists;
            set
            {
                if (value != _tableExists)
                {
                    _tableExists = value;
                    OnPropertyChanged(nameof(TableExists));
                }
            }
        }
        public IEnumerable<EncounterType> GroupNames { get; private set; }
        private int _selectedGroup = -1;
        public int SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                // #34, do not check for != because then we do not load the new table when _grp changes
                _selectedGroup = value;
                OnPropertyChanged(nameof(SelectedGroup));
                LoadEncounterTable(value == -1 ? null : _grp.Groups[value].Table);
            }
        }
        private int _selectedTable = -1;
        public int SelectedTable
        {
            get => _selectedTable;
            set
            {
                if (value != _selectedTable)
                {
                    _selectedTable = value;
                    OnPropertyChanged(nameof(SelectedTable));
                    if (value == -1)
                    {
                        LoadEncounterTable(null);
                    }
                    else
                    {
                        var tbl = EncounterTable.LoadOrGet(value);
                        _grp.Groups[_selectedGroup].Table = tbl;
                        LoadEncounterTable(tbl);
                    }
                }
            }
        }

        public ObservableCollection<EncounterModel> Encounters { get; } = new ObservableCollection<EncounterModel>();

        private EncounterGroups _grp;
        private EncounterTable _tbl;

        public EncounterEditor()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);
        }

        internal void SetEncounterGroup(EncounterGroups grp)
        {
            _grp = grp;
            UpdateGroups();
        }
        private void LoadEncounterTable(EncounterTable tbl)
        {
            if (_tbl == tbl)
            {
                return;
            }
            _tbl = tbl;
            DisposeEncounters();
            Encounters.Clear();
            if (tbl is null)
            {
                SelectedTable = -1;
                TableExists = false;
                return;
            }
            foreach (EncounterTable.Encounter e in tbl.Encounters)
            {
                AddEncModel(new EncounterModel(e));
            }
            UpdateAddEncounterEnabled();
            UpdateNumEncountersText();
            OnPropertyChanged(nameof(Chance));
            UpdateChanceProbabilityText();
            UpdateEncounterChanceProbabilities();
            SelectedTable = tbl.Id;
            TableExists = true;
        }
        public async void AddEncounterGroup()
        {
            Window window = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
            var dialog = new SelectSomethingDialog(Utils.GetEnumValues<EncounterType>().Where(t => !_grp.Groups.Any(g => g.Type == t)));
            object result = await dialog.ShowDialog<object>(window);
            if (result is null)
            {
                return;
            }
            var tbl = EncounterTable.LoadOrGet(0);
            int index = _grp.Groups.Count;
            _grp.Groups.Insert(index, new EncounterGroups.EncounterGroup((EncounterType)result, tbl));
            UpdateGroupNames();
            SelectedGroup = index;
        }
        public void RemoveEncounterGroup()
        {
            _grp.Groups.RemoveAt(_selectedGroup);
            UpdateGroups();
        }
        public static async void CreateEncounterTable()
        {
            Window window = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
            var dialog = new NewAssetDialog(EncounterTable.Ids);
            string result = await dialog.ShowDialog<string>(window);
            if (result is null)
            {
                return;
            }
            _ = new EncounterTable(result);
            await MessageBox.Show($"An encounter table named \"{result}\" was created.", "Success!", MessageBox.MessageBoxButtons.Ok, owner: window);
        }
        public void SaveEncounterTable()
        {
            _tbl.Save();
        }

        private void AddEncModel(EncounterModel e)
        {
            Encounters.Add(e);
            e.PropertyChanged += EncounterUpdated;
            e.RemoveReady += RemoveEncounter;
        }
        public void AddEncounter()
        {
            int index = _tbl.Encounters.Count;
            var e = new EncounterTable.Encounter();
            _tbl.Encounters.Insert(index, e);
            var m = new EncounterModel(e);
            AddEncModel(m);
            UpdateAddEncounterEnabled();
            UpdateNumEncountersText();
            UpdateEncounterChanceProbabilities();
        }
        private void EncounterUpdated(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(EncounterModel.Chance))
            {
                UpdateEncounterChanceProbabilities();
            }
        }
        private void RemoveEncounter(object sender, EventArgs e)
        {
            var enc = (EncounterModel)sender;
            enc.Dispose();
            Encounters.Remove(enc);
            _tbl.Encounters.Remove(enc.Encounter);
            UpdateAddEncounterEnabled();
            UpdateNumEncountersText();
            UpdateEncounterChanceProbabilities();
        }
        private void UpdateAddEncounterEnabled()
        {
            AddEncounterEnabled = _tbl.Encounters.Count < byte.MaxValue;
        }
        private void UpdateNumEncountersText()
        {
            NumEncountersText = $"{_tbl.Encounters.Count}/{byte.MaxValue} Encounters";
        }

        private void UpdateGroupNames()
        {
            // Do not OrderBy because then we select the wrong group index
            GroupNames = _grp.Groups.Select(t => t.Type);//.OrderBy(e => e.ToString());
            OnPropertyChanged(nameof(GroupNames));
        }
        private void UpdateGroups()
        {
            UpdateGroupNames();
            SelectedGroup = _grp.Groups.Count > 0 ? 0 : -1;
        }
        private void UpdateChanceProbabilityText()
        {
            ChanceProbability = "Probability of phenomenon: " + _tbl.GetChanceOfPhenomenon().ToString("P2");
            OnPropertyChanged(nameof(ChanceProbability));
        }
        private void UpdateEncounterChanceProbabilities()
        {
            float perStep = _tbl.GetChanceOfPhenomenon();
            ushort combined = _tbl.GetCombinedChance();
            foreach (EncounterModel e in Encounters)
            {
                e.UpdateChanceProbability(perStep, combined);
            }
        }

        private void DisposeEncounters()
        {
            foreach (EncounterModel e in Encounters)
            {
                e.Dispose();
            }
        }

        public void Dispose()
        {
            PropertyChanged = null;
            DisposeEncounters();
        }
    }
}
