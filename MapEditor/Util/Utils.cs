using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kermalis.MapEditor.Util
{
    internal static class Utils
    {
        public static IPlatformRenderInterface RenderInterface { get; } = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

        // Temporary fix for https://github.com/AvaloniaUI/Avalonia/issues/3079
        public static bool TemporaryFix_PointerInControl(this Rect rect, Point pos)
        {
            double x = pos.X;
            double y = pos.Y;
            return x >= 0 && x < rect.Width && y >= 0 && y < rect.Height;
        }
        // Temporary fix for https://github.com/AvaloniaUI/Avalonia/issues/2975
        public static void TemporaryFix_Activate(this Window window)
        {
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }
            window.Activate();
        }
        public static void ForceRedraw(this ComboBox c)
        {
            IBrush old = c.Background;
            c.Background = old.Equals(Brushes.AliceBlue) ? Brushes.AntiqueWhite : Brushes.AliceBlue;
            c.Background = old;
        }

        public static readonly Regex InvalidFileNameRegex = new("[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]");

        public static IEnumerable<TEnum> GetEnumValues<TEnum>() where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>().OrderBy(e => e.ToString());
        }

        public static TEnum EnumValue<TEnum>(this JToken j) where TEnum : struct, Enum
        {
            Type type = typeof(TEnum);
            // If it has the [Flags] attribute, read a series of bools
            if (type.IsDefined(typeof(FlagsAttribute), false))
            {
                ulong value = 0;
                foreach (TEnum flag in Enum.GetValues<TEnum>())
                {
                    ulong ulFlag = Convert.ToUInt64(flag);
                    if (ulFlag != 0uL && j[flag.ToString()].Value<bool>())
                    {
                        value |= ulFlag;
                    }
                }
                return (TEnum)Enum.ToObject(type, value);
            }
            else
            {
                return Enum.Parse<TEnum>(j.Value<string>());
            }
        }
        public static void WriteEnum<TEnum>(this JsonTextWriter w, TEnum value) where TEnum : struct, Enum
        {
            // If it has the [Flags] attribute, write a series of bools
            if (typeof(TEnum).IsDefined(typeof(FlagsAttribute), false))
            {
#pragma warning disable CA2248 // Provide correct 'enum' argument to 'Enum.HasFlag'
                w.WriteStartObject();
                foreach (TEnum flag in GetEnumValues<TEnum>())
                {
                    if (Convert.ToUInt64(flag) != 0uL)
                    {
                        w.WritePropertyName(flag.ToString());
                        w.WriteValue(value.HasFlag(flag));
                    }
                }
                w.WriteEndObject();
#pragma warning restore CA2248 // Provide correct 'enum' argument to 'Enum.HasFlag'
            }
            else
            {
                w.WriteValue(value.ToString());
            }
        }
    }
}
