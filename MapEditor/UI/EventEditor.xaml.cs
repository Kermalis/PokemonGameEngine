using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.UI.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Kermalis.MapEditor.UI
{
    public sealed class EventEditor : UserControl, IDisposable, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<WarpModel> Warps { get; } = new ObservableCollection<WarpModel>();

        private WarpModel _selectedWarp;
        public WarpModel SelectedWarp
        {
            get => _selectedWarp;
            set
            {
                if (value != _selectedWarp)
                {
                    _selectedWarp = value;
                    OnPropertyChanged(nameof(SelectedWarp));
                }
            }
        }
        private bool _addWarpEnabled;
        public bool AddWarpEnabled
        {
            get => _addWarpEnabled;
            set
            {
                if (value != _addWarpEnabled)
                {
                    _addWarpEnabled = value;
                    OnPropertyChanged(nameof(AddWarpEnabled));
                }
            }
        }
        private string _numWarpsText;
        public string NumWarpsText
        {
            get => _numWarpsText;
            private set
            {
                if (value != _numWarpsText)
                {
                    _numWarpsText = value;
                    OnPropertyChanged(nameof(NumWarpsText));
                }
            }
        }

        private readonly EventsImage _eventsImage;

        public EventEditor()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _eventsImage = this.FindControl<EventsImage>("EventsImage");
        }

        internal void SetMap(Map map)
        {
            _eventsImage.Map = map;
            DisposeModels();
            Map.Events events = map.MapEvents;
            foreach (Map.Events.WarpEvent warp in events.Warps)
            {
                var wm = new WarpModel(warp);
                wm.PropertyChanged += Warp_PropertyChanged;
                Warps.Add(wm);
            }
            SelectedWarp = Warps.FirstOrDefault();
            UpdateNumWarps();
            _eventsImage.InvalidateVisual();
        }

        private void Warp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Update visual
            switch (e.PropertyName)
            {
                case nameof(WarpModel.X):
                case nameof(WarpModel.Y): _eventsImage.InvalidateVisual(); break;
            }
        }

        public void AddWarp()
        {
            var warp = new Map.Events.WarpEvent();
            _eventsImage.Map.MapEvents.Warps.Add(warp);
            var wm = new WarpModel(warp);
            wm.PropertyChanged += Warp_PropertyChanged;
            Warps.Add(wm);
            SelectedWarp = wm;
            UpdateNumWarps();
            _eventsImage.InvalidateVisual();
        }
        public void RemoveWarp()
        {
            WarpModel wm = _selectedWarp;
            Warps.Remove(wm);
            _eventsImage.Map.MapEvents.Warps.Remove(wm.Warp);
            wm.Dispose();
            SelectedWarp = Warps.FirstOrDefault();
            UpdateNumWarps();
            _eventsImage.InvalidateVisual();
        }
        private void UpdateNumWarps()
        {
            int count = _eventsImage.Map.MapEvents.Warps.Count;
            AddWarpEnabled = count < ushort.MaxValue;
            NumWarpsText = $"{count}/{ushort.MaxValue} Warps";
        }

        private void DisposeModels()
        {
            foreach (WarpModel warp in Warps)
            {
                warp.Dispose();
            }
            Warps.Clear();
            _eventsImage.InvalidateVisual();
        }

        public void Dispose()
        {
            PropertyChanged = null;
            DisposeModels();
            _eventsImage.Dispose();
        }
    }
}
