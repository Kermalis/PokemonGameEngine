using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonGameEngine.Overworld;
using System.Collections.Generic;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class MapDetailsEditor : UserControl, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        public static IEnumerable<MapSection> SelectableMapSections { get; } = Utils.GetEnumValues<MapSection>();
        public static IEnumerable<MapWeather> SelectableWeathers { get; } = Utils.GetEnumValues<MapWeather>();
        public static IEnumerable<Song> SelectableSongs { get; } = Utils.GetEnumValues<Song>();

        public MapFlags Flags
        {
            get => _details is null ? MapFlags.None : _details.Flags;
            set
            {
                if (value != _details.Flags)
                {
                    _details.Flags = value;
                    _converter.Flags = value;
                    OnPropertyChanged(nameof(Flags));
                }
            }
        }
        public MapSection Section
        {
            get => _details is null ? MapSection.None : _details.Section;
            set
            {
                if (value != _details.Section)
                {
                    _details.Section = value;
                    OnPropertyChanged(nameof(Section));
                }
            }
        }
        public MapWeather Weather
        {
            get => _details is null ? MapWeather.None : _details.Weather;
            set
            {
                if (value != _details.Weather)
                {
                    _details.Weather = value;
                    OnPropertyChanged(nameof(Weather));
                }
            }
        }
        public Song Music
        {
            get => _details is null ? Song.None : _details.Music;
            set
            {
                if (value != _details.Music)
                {
                    _details.Music = value;
                    OnPropertyChanged(nameof(Music));
                }
            }
        }

        private Map.Details _details;
        private readonly MapFlagsConverter _converter;

        public MapDetailsEditor()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _converter = (MapFlagsConverter)this.FindResource("MapFlagsConverter");
        }

        internal void SetDetails(Map.Details details)
        {
            _details = details;
            _converter.Flags = details.Flags;
            OnPropertyChanged(nameof(Flags));
            OnPropertyChanged(nameof(Section));
            OnPropertyChanged(nameof(Weather));
            OnPropertyChanged(nameof(Music));
        }
    }
}
