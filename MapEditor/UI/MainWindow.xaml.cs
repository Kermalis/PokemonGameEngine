using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class MainWindow : Window, IDisposable, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

#pragma warning disable IDE0069 // Disposable fields should be disposed
        private BlocksetEditor _blocksetEditor;
#pragma warning restore IDE0069 // Disposable fields should be disposed

        private readonly LayoutEditor _layoutEditor;
        private readonly MovementEditor _movementEditor;
        private readonly EventEditor _eventEditor;
        private readonly ConnectionEditor _connectionEditor;
        private readonly EncounterEditor _encounterEditor;
        private readonly MapDetailsEditor _mapDetailsEditor;

        private Map _map;
        private string _selectedMap;
        public string SelectedMap
        {
            get => _selectedMap;
            set
            {
                if (IsInitialized && value != _selectedMap)
                {
                    _selectedMap = value;
                    var map = Map.LoadOrGet(value);
                    _map = map;
                    Map.Layout ml = map.MapLayout;
                    _layoutEditor.SetLayout(ml);
                    _movementEditor.SetLayout(ml);
                    _eventEditor.SetMap(map);
                    _connectionEditor.SetMap(map);
                    _encounterEditor.SetEncounterGroup(map.Encounters);
                    _mapDetailsEditor.SetDetails(map.MapDetails);
                    OnPropertyChanged(nameof(SelectedMap));
                }
            }
        }

        public MainWindow()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _layoutEditor = this.FindControl<LayoutEditor>("LayoutEditor");
            _movementEditor = this.FindControl<MovementEditor>("MovementEditor");
            _eventEditor = this.FindControl<EventEditor>("EventEditor");
            _connectionEditor = this.FindControl<ConnectionEditor>("ConnectionEditor");
            _encounterEditor = this.FindControl<EncounterEditor>("EncounterEditor");
            _mapDetailsEditor = this.FindControl<MapDetailsEditor>("MapDetailsEditor");
            SelectedMap = Map.Ids[0];
        }

        public void OpenBlocksetEditor()
        {
            if (_blocksetEditor != null)
            {
                _blocksetEditor.TemporaryFix_Activate();
            }
            else
            {
                _blocksetEditor = new BlocksetEditor();
                _blocksetEditor.Show();
                _blocksetEditor.Closed += BlocksetEditor_Closed;
            }
        }
        private void BlocksetEditor_Closed(object sender, EventArgs e)
        {
            _blocksetEditor.Closed -= BlocksetEditor_Closed;
            _blocksetEditor = null;
        }

        public void SaveLayout()
        {
            _map.MapLayout.Save();
        }
        public void SaveMap()
        {
            _map.Save();
        }

        protected override bool HandleClosing()
        {
            Dispose();
            return base.HandleClosing();
        }
        public void Dispose()
        {
            PropertyChanged = null;
            _layoutEditor.Dispose();
            _movementEditor.Dispose();
            _eventEditor.Dispose();
            _connectionEditor.Dispose();
            _encounterEditor.Dispose();
            _blocksetEditor?.Close();
            _map.MapLayout.Dispose();
        }
    }
}
