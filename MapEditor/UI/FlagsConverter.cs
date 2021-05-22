using Avalonia.Data.Converters;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Globalization;

namespace Kermalis.MapEditor.UI
{
    public abstract class FlagsConverter<TEnum> : IValueConverter where TEnum : struct, Enum
    {
        internal TEnum Flags;

        private static TEnum GetFlag(object parameter)
        {
            return Enum.Parse<TEnum>((string)parameter);
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
#pragma warning disable CA2248 // Provide correct 'enum' argument to 'Enum.HasFlag'
            return Flags.HasFlag(GetFlag(parameter));
#pragma warning restore CA2248 // Provide correct 'enum' argument to 'Enum.HasFlag'
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            TEnum flag = GetFlag(parameter);
            if (b)
            {
                Flags = (TEnum)Enum.ToObject(typeof(TEnum), System.Convert.ToUInt64(Flags) | System.Convert.ToUInt64(flag));
            }
            else
            {
                Flags = (TEnum)Enum.ToObject(typeof(TEnum), System.Convert.ToUInt64(Flags) & ~System.Convert.ToUInt64(flag));
            }
            return Flags;
        }
    }
    public sealed class MapFlagsConverter : FlagsConverter<MapFlags>
    {
    }
    public sealed class PassageConverter : FlagsConverter<LayoutBlockPassage>
    {
    }
}
