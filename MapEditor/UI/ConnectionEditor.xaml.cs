using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.UI.Models;
using Kermalis.MapEditor.Util;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class ConnectionEditor : UserControl, IDisposable, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        private string _numConnectionsText;
        public string NumConnectionsText
        {
            get => _numConnectionsText;
            private set
            {
                if (value != _numConnectionsText)
                {
                    _numConnectionsText = value;
                    OnPropertyChanged(nameof(NumConnectionsText));
                }
            }
        }
        private bool _addEnabled;
        public bool AddEnabled
        {
            get => _addEnabled;
            private set
            {
                if (_addEnabled != value)
                {
                    _addEnabled = value;
                    OnPropertyChanged(nameof(AddEnabled));
                }
            }
        }
        private bool _hasSelection;
        public bool HasSelection
        {
            get => _hasSelection;
            private set
            {
                if (_hasSelection != value)
                {
                    _hasSelection = value;
                    OnPropertyChanged(nameof(HasSelection));
                }
            }
        }
        private int _selectedConnection = -1;
        public int SelectedConnection
        {
            get => _selectedConnection;
            set
            {
                if (_selectedConnection != value)
                {
                    if (_selectedConnection != -1 && _selectedConnection < _map.Connections.Count)
                    {
                        Maps[_selectedConnection + 1].Select(false);
                    }
                    _selectedConnection = value;
                    if (value != -1)
                    {
                        UpdateSelectionDetails();
                    }
                    OnPropertyChanged(nameof(SelectedConnection));
                    HasSelection = value != -1;
                }
            }
        }

        private bool _switching;
        public int _selectedDirection;
        public int SelectedDirection
        {
            get => _selectedDirection;
            set
            {
                if (value != -1 && _selectedDirection != value)
                {
                    _selectedDirection = value;
                    if (!_switching)
                    {
                        _map.Connections[_selectedConnection].Dir = (Map.Connection.Direction)value;
                        ArrangeMaps();
                    }
                    OnPropertyChanged(nameof(SelectedDirection));
                }
            }
        }
        public double _offset;
        public double Offset
        {
            get => _offset;
            set
            {
                if (_offset != value)
                {
                    _offset = value;
                    if (!_switching)
                    {
                        _map.Connections[_selectedConnection].Offset = (int)value;
                        ArrangeMaps();
                    }
                    OnPropertyChanged(nameof(Offset));
                }
            }
        }
        public int _selectedMap;
        public int SelectedMap
        {
            get => _selectedMap;
            set
            {
                if (value != -1 && _selectedMap != value)
                {
                    _selectedMap = value;
                    if (!_switching)
                    {
                        _map.Connections[_selectedConnection].MapId = value;
                        ConnectionModel c = Maps[_selectedConnection + 1];
                        c.Map.MapLayout.OnDrew -= MapLayout_OnDrew;
                        c.SetMap(Map.LoadOrGet(value));
                        ArrangeMaps();
                    }
                    OnPropertyChanged(nameof(SelectedMap));
                }
            }
        }

        private IBrush _backgroundBrush;
        public IBrush BackgroundBrush
        {
            get => _backgroundBrush;
            private set
            {
                if (_backgroundBrush?.Equals(value) != true)
                {
                    _backgroundBrush = value;
                    OnPropertyChanged(nameof(BackgroundBrush));
                }
            }
        }
        private double _panelWidth;
        public double PanelWidth
        {
            get => _panelWidth;
            private set
            {
                if (_panelWidth != value)
                {
                    _panelWidth = value;
                    OnPropertyChanged(nameof(PanelWidth));
                }
            }
        }
        private double _panelHeight;
        public double PanelHeight
        {
            get => _panelHeight;
            private set
            {
                if (_panelHeight != value)
                {
                    _panelHeight = value;
                    OnPropertyChanged(nameof(PanelHeight));
                }
            }
        }

        public Array Directions { get; } = Enum.GetValues(typeof(Map.Connection.Direction));
        public ObservableCollection<ConnectionModel> Maps { get; } = new ObservableCollection<ConnectionModel>();

        private readonly ItemsControl _mapsItemsControl;
        private Map _map;

        public ConnectionEditor()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _mapsItemsControl = this.FindControl<ItemsControl>("MapsItemsControl");
            _mapsItemsControl.PointerPressed += MapsItemsControl_PointerPressed;
        }

        private void UpdateSelectionDetails()
        {
            Maps[_selectedConnection + 1].Select(true);
            Map.Connection c = _map.Connections[_selectedConnection];
            _switching = true;
            SelectedDirection = (int)c.Dir;
            SelectedMap = c.MapId;
            Offset = c.Offset;
            _switching = false;
        }

        internal void SetMap(Map map)
        {
            SelectedConnection = -1;
            for (int i = Maps.Count - 1; i >= 0; i--)
            {
                Remove(i);
            }
            _map = map;
            BackgroundBrush = new ImageBrush(map.MapLayout.BorderBlocksBitmap) { Stretch = Stretch.None, TileMode = TileMode.Tile, DestinationRect = new RelativeRect(0, 0, 32, 32, RelativeUnit.Absolute) };
            Add(map);
            int count = map.Connections.Count;
            if (count != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    Add(Map.LoadOrGet(map.Connections[i].MapId));
                }
                SelectedConnection = 0;
            }
            ArrangeMaps();
            UpdateNumConnections();
        }

        public void AddButton()
        {
            int index = _map.Connections.Count;
            var c = new Map.Connection();
            _map.Connections.Insert(index, c);
            Add(Map.LoadOrGet(c.MapId));
            SelectedConnection = index;
            ArrangeMaps();
            UpdateNumConnections();
        }
        public void RemoveButton()
        {
            _map.Connections.RemoveAt(_selectedConnection);
            Remove(_selectedConnection + 1);
            if (_selectedConnection >= _map.Connections.Count)
            {
                SelectedConnection = _map.Connections.Count - 1;
            }
            else
            {
                UpdateSelectionDetails();
            }
            ArrangeMaps();
            UpdateNumConnections();
        }
        private void Add(Map map)
        {
            map.MapLayout.OnDrew += MapLayout_OnDrew;
            Maps.Add(new ConnectionModel(map, map == _map));
        }
        private void Remove(int index)
        {
            ConnectionModel c = Maps[index];
            c.Dispose();
            c.Map.MapLayout.OnDrew -= MapLayout_OnDrew;
            Maps.Remove(c);
        }
        private void UpdateNumConnections()
        {
            int count = _map.Connections.Count;
            AddEnabled = count < byte.MaxValue;
            NumConnectionsText = $"{count}/{byte.MaxValue} Connections";
        }
        private void ArrangeMaps()
        {
            Map.Layout ml = _map.MapLayout;
            int mWidth = ml.Width * 16;
            int mHeight = ml.Height * 16;
            int mostUp = 0;
            int mostLeft = 0;
            int mostRight = 0;
            int mostDown = 0;
            int count = Maps.Count;
            for (int i = 1; i < count; i++)
            {
                void Down(int down)
                {
                    if (down > mostDown)
                    {
                        mostDown = down;
                    }
                }
                void Up(int up)
                {
                    if (up > mostUp)
                    {
                        mostUp = up;
                    }
                }
                void Left(int left)
                {
                    if (left > mostLeft)
                    {
                        mostLeft = left;
                    }
                }
                void Right(int right)
                {
                    if (right > mostRight)
                    {
                        mostRight = right;
                    }
                }
                void Horizontal(int off, int width)
                {
                    Left(-off * 16);
                    Right((off * 16) + (width * 16) - mWidth);
                }
                void Vertical(int off, int height)
                {
                    Up(-off * 16);
                    Down((off * 16) + (height * 16) - mHeight);
                }
                ConnectionModel cm = Maps[i];
                Map.Layout cml = cm.Map.MapLayout;
                Map.Connection c = _map.Connections[i - 1];
                switch (c.Dir)
                {
                    case Map.Connection.Direction.South:
                    {
                        Down(cml.Height * 16);
                        Horizontal(c.Offset, cml.Width);
                        break;
                    }
                    case Map.Connection.Direction.North:
                    {
                        Up(cml.Height * 16);
                        Horizontal(c.Offset, cml.Width);
                        break;
                    }
                    case Map.Connection.Direction.West:
                    {
                        Left(cml.Width * 16);
                        Vertical(c.Offset, cml.Height);
                        break;
                    }
                    case Map.Connection.Direction.East:
                    {
                        Right(cml.Width * 16);
                        Vertical(c.Offset, cml.Height);
                        break;
                    }
                }
            }
            Maps[0].Position = new Point(mostLeft, mostUp);
            for (int i = 1; i < count; i++)
            {
                ConnectionModel cm = Maps[i];
                Map.Layout cml = cm.Map.MapLayout;
                Map.Connection c = _map.Connections[i - 1];
                switch (c.Dir)
                {
                    case Map.Connection.Direction.South:
                    {
                        cm.Position = new Point(mostLeft + (c.Offset * 16), mostUp + mHeight + (mostDown - (cml.Height * 16)));
                        break;
                    }
                    case Map.Connection.Direction.North:
                    {
                        cm.Position = new Point(mostLeft + (c.Offset * 16), mostUp - (cml.Height * 16));
                        break;
                    }
                    case Map.Connection.Direction.West:
                    {
                        cm.Position = new Point(mostLeft - (cml.Width * 16), mostUp + (c.Offset * 16));
                        break;
                    }
                    case Map.Connection.Direction.East:
                    {
                        cm.Position = new Point(mostLeft + mWidth + (mostRight - (cml.Width * 16)), mostUp + (c.Offset * 16));
                        break;
                    }
                }
            }
            PanelWidth = mostLeft + mWidth + mostRight;
            PanelHeight = mostUp + mHeight + mostDown;
        }

        private void MapLayout_OnDrew(Map.Layout layout, bool drewBorderBlocks, bool wasResized)
        {
            if (!drewBorderBlocks && wasResized)
            {
                ArrangeMaps();
            }
            if (!drewBorderBlocks || layout == _map.MapLayout)
            {
                _mapsItemsControl.InvalidateVisual();
            }
        }

        private void MapsItemsControl_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (_map != null)
            {
                PointerPoint pp = e.GetCurrentPoint(_mapsItemsControl);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
                {
                    Point pos = pp.Position;
                    if (_mapsItemsControl.Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        double x = pos.X;
                        double y = pos.Y;
                        int count = Maps.Count;
                        for (int i = 1; i < count; i++)
                        {
                            ConnectionModel cm = Maps[i];
                            Map.Layout cml = cm.Map.MapLayout;
                            Point cmp = cm.Position;
                            double cmx = cmp.X;
                            double cmy = cmp.Y;
                            if (x >= cmx && x < cmx + (cml.Width * 16) && y >= cmy && y < cmy + (cml.Height * 16))
                            {
                                SelectedConnection = i - 1;
                                e.Handled = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            PropertyChanged = null;
            for (int i = Maps.Count - 1; i >= 0; i--)
            {
                ConnectionModel c = Maps[i];
                c.Dispose();
                c.Map.MapLayout.OnDrew -= MapLayout_OnDrew;
            }
            _mapsItemsControl.PointerPressed -= MapsItemsControl_PointerPressed;
        }
    }
}
