using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Kermalis.PokemonBattleEngine.Data;
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
        public static string[] GetOrderedFormStrings(IReadOnlyList<PBEForm> strs, PBESpecies species)
        {
            return strs.OrderBy(f => f).Select(f => PBEDataProvider.Instance.GetFormName(species, f).FromGlobalLanguage()).ToArray();
        }

        public static TEnum ReadEnumValue<TEnum>(this JToken j) where TEnum : struct, Enum
        {
            return Enum.Parse<TEnum>(j.Value<string>());
        }
        public static TEnum ReadFlagsEnumValue<TEnum>(this JToken j) where TEnum : struct, Enum
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
            return (TEnum)Enum.ToObject(typeof(TEnum), value);
        }
        public static void WriteEnum<TEnum>(this JsonTextWriter w, TEnum value) where TEnum : struct, Enum
        {
            w.WriteValue(value.ToString());
        }
        // If it has the [Flags] attribute, write a series of bools
        public static void WriteFlagsEnum<TEnum>(this JsonTextWriter w, TEnum value) where TEnum : struct, Enum
        {
            w.WriteStartObject();
            foreach (TEnum flag in GetEnumValues<TEnum>())
            {
                if (Convert.ToUInt64(flag) != 0uL)
                {
                    w.WritePropertyName(flag.ToString());
#pragma warning disable CA2248 // Provide correct 'enum' argument to 'Enum.HasFlag'
                    w.WriteValue(value.HasFlag(flag));
#pragma warning restore CA2248 // Provide correct 'enum' argument to 'Enum.HasFlag'
                }
            }
            w.WriteEndObject();
        }
    }
}
