using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Kermalis.MapEditor.UI
{
    public sealed class ElevationConverter : IValueConverter
    {
        internal byte Elevations;

        private int GetFlag(object parameter)
        {
            return 1 << System.Convert.ToInt32(parameter);
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Elevations & GetFlag(parameter)) != 0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            int flag = GetFlag(parameter);
            if (b)
            {
                Elevations = (byte)(Elevations | flag);
            }
            else
            {
                Elevations = (byte)(Elevations & ~flag);
            }
            return Elevations;
        }
    }
}
