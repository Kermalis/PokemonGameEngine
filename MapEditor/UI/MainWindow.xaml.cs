using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.Reactive;

namespace Kermalis.MapEditor.UI
{
    public sealed class MainWindow : Window, IDisposable, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        public ReactiveCommand<Unit, Unit> SaveMapCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenBlocksetEditorCommand { get; }

#pragma warning disable IDE0069 // Disposable fields should be disposed
        private BlocksetEditor _blocksetEditor;
#pragma warning restore IDE0069 // Disposable fields should be disposed

        private readonly MapEditor _mapEditor;
        private readonly ConnectionEditor _connectionEditor;

        private Map _map;
        private string _selectedMap = null;
        public string SelectedMap
        {
            get => _selectedMap;
            set
            {
                if (value != _selectedMap)
                {
                    _selectedMap = value;
                    if (IsInitialized)
                    {
                        OnMapChanged();
                    }
                    OnPropertyChanged(nameof(SelectedMap));
                }
            }
        }

        public MainWindow()
        {
            SaveMapCommand = ReactiveCommand.Create(SaveMap);
            OpenBlocksetEditorCommand = ReactiveCommand.Create(OpenBlocksetEditor);

            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _mapEditor = this.FindControl<MapEditor>("MapEditor");
            _connectionEditor = this.FindControl<ConnectionEditor>("ConnectionEditor");
            OnMapChanged();
        }

        private void OnMapChanged()
        {
            _map = Map.LoadOrGet(_selectedMap);
            _mapEditor.SetMap(_map);
            _connectionEditor.SetMap(_map);
        }

        private void OpenBlocksetEditor()
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

        private void SaveMap()
        {
            _map.MapLayout.Save();
            _map.Save();
        }

        protected override bool HandleClosing()
        {
            Dispose();
            return base.HandleClosing();
        }

        public void Dispose()
        {
            _mapEditor.Dispose();
            _connectionEditor.Dispose();
            SaveMapCommand.Dispose();
            OpenBlocksetEditorCommand.Dispose();
            _blocksetEditor?.Close();
            _map.MapLayout.Dispose();
        }
    }
}
