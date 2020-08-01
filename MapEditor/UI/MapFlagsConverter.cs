using Avalonia.Data.Converters;
using Kermalis.PokemonGameEngine.Overworld;
using System;
using System.Globalization;

namespace Kermalis.MapEditor.UI
{
    public sealed class MapFlagsConverter : IValueConverter
    {
        internal MapFlags Flags;

        private MapFlags GetFlag(object parameter)
        {
            return (MapFlags)Enum.Parse(typeof(MapFlags), (string)parameter);
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Flags.HasFlag(GetFlag(parameter));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            MapFlags flag = GetFlag(parameter);
            if (b)
            {
                Flags |= flag;
            }
            else
            {
                Flags &= ~flag;
            }
            return Flags;
        }
    }
}
