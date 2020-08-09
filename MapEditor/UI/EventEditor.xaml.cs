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

        public ObservableCollection<ObjectEventModel> Objects { get; } = new ObservableCollection<ObjectEventModel>();
        private ObjectEventModel _selectedObject;
        public ObjectEventModel SelectedObject
        {
            get => _selectedObject;
            set
            {
                if (value != _selectedObject)
                {
                    _selectedObject = value;
                    OnPropertyChanged(nameof(SelectedObject));
                }
            }
        }
        private bool _addObjectEnabled;
        public bool AddObjectEnabled
        {
            get => _addObjectEnabled;
            set
            {
                if (value != _addObjectEnabled)
                {
                    _addObjectEnabled = value;
                    OnPropertyChanged(nameof(AddObjectEnabled));
                }
            }
        }
        private string _numObjectsText;
        public string NumObjectsText
        {
            get => _numObjectsText;
            private set
            {
                if (value != _numObjectsText)
                {
                    _numObjectsText = value;
                    OnPropertyChanged(nameof(NumObjectsText));
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
            foreach (Map.Events.ObjEvent obj in events.Objs)
            {
                var om = new ObjectEventModel(obj);
                om.PropertyChanged += Object_PropertyChanged;
                Objects.Add(om);
            }
            SelectedObject = Objects.FirstOrDefault();
            UpdateNumObjects();
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

        private void Object_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Update visual
            switch (e.PropertyName)
            {
                case nameof(ObjectEventModel.X):
                case nameof(ObjectEventModel.Y): _eventsImage.InvalidateVisual(); break;
            }
        }
        public void AddObject()
        {
            var obj = new Map.Events.ObjEvent();
            _eventsImage.Map.MapEvents.Objs.Add(obj);
            var om = new ObjectEventModel(obj);
            om.PropertyChanged += Object_PropertyChanged;
            Objects.Add(om);
            SelectedObject = om;
            UpdateNumObjects();
            _eventsImage.InvalidateVisual();
        }
        public void RemoveObject()
        {
            ObjectEventModel om = _selectedObject;
            Objects.Remove(om);
            _eventsImage.Map.MapEvents.Objs.Remove(om.Obj);
            om.Dispose();
            SelectedObject = Objects.FirstOrDefault();
            UpdateNumObjects();
            _eventsImage.InvalidateVisual();
        }
        private void UpdateNumObjects()
        {
            int count = _eventsImage.Map.MapEvents.Objs.Count;
            AddObjectEnabled = count < ushort.MaxValue;
            NumObjectsText = $"{count}/{ushort.MaxValue} Objects";
        }

        private void DisposeModels()
        {
            foreach (WarpModel warp in Warps)
            {
                warp.Dispose();
            }
            Warps.Clear();
            foreach (ObjectEventModel obj in Objects)
            {
                obj.Dispose();
            }
            Objects.Clear();
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
