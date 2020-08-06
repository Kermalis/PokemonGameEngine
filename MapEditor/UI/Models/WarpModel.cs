using Kermalis.MapEditor.Core;
using System;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI.Models
{
    public sealed class WarpModel : INotifyPropertyChanged, IDisposable
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int X
        {
            get => Warp.X;
            set
            {
                if (value != Warp.X)
                {
                    Warp.X = value;
                    OnPropertyChanged(nameof(X));
                }
            }
        }
        public int Y
        {
            get => Warp.Y;
            set
            {
                if (value != Warp.Y)
                {
                    Warp.Y = value;
                    OnPropertyChanged(nameof(Y));
                }
            }
        }
        public byte Elevation
        {
            get => Warp.Elevation;
            set
            {
                if (value != Warp.Elevation)
                {
                    Warp.Elevation = value;
                    OnPropertyChanged(nameof(Elevation));
                }
            }
        }
        public string DestMap
        {
            get => Warp.DestMap;
            set
            {
                if (value != Warp.DestMap)
                {
                    Warp.DestMap = value;
                    OnPropertyChanged(nameof(DestMap));
                }
            }
        }
        public int DestX
        {
            get => Warp.DestX;
            set
            {
                if (value != Warp.DestX)
                {
                    Warp.DestX = value;
                    OnPropertyChanged(nameof(DestX));
                }
            }
        }
        public int DestY
        {
            get => Warp.DestY;
            set
            {
                if (value != Warp.DestY)
                {
                    Warp.DestY = value;
                    OnPropertyChanged(nameof(DestY));
                }
            }
        }
        public byte DestElevation
        {
            get => Warp.DestElevation;
            set
            {
                if (value != Warp.DestElevation)
                {
                    Warp.DestElevation = value;
                    OnPropertyChanged(nameof(DestElevation));
                }
            }
        }

        internal readonly Map.Events.WarpEvent Warp;

        internal WarpModel(Map.Events.WarpEvent warp)
        {
            Warp = warp;
        }

        public void Dispose()
        {
            PropertyChanged = null;
        }
    }
}
