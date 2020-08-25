using System;

namespace Kermalis.PokemonGameEngine.World
{
    internal static class OverworldTime
    {
        public static Month? OverrideMonth { get; set; }
        public static int? OverrideHour { get; set; }
        public static int? OverrideMinute { get; set; }

        public static Month GetMonth(Month month)
        {
            return OverrideMonth ?? month;
        }
        public static int GetHour(int hour)
        {
            return OverrideHour ?? hour;
        }
        public static int GetMinute(int minute)
        {
            return OverrideMinute ?? minute;
        }

        // Northern hemisphere
        public static Season GetSeason(Month month)
        {
            switch (month)
            {
                case Month.December:
                case Month.January:
                case Month.February: return Season.Winter;
                case Month.March:
                case Month.April:
                case Month.May: return Season.Spring;
                case Month.June:
                case Month.July:
                case Month.August: return Season.Summer;
                case Month.September:
                case Month.October:
                case Month.November: return Season.Autumn;
                default: throw new ArgumentOutOfRangeException(nameof(month));
            }
        }
        // Evolutions only check for "Night", so day evolutions occur in Morning, Day, and Evening
        public static TimeOfDay GetTimeOfDay(Season season, int hour)
        {
            switch (season)
            {
                case Season.Spring:
                {
                    if (hour >= 20 || hour <= 4)
                    {
                        return TimeOfDay.Night; // 8:00PM - 4:59AM
                    }
                    if (hour >= 17)
                    {
                        return TimeOfDay.Evening; // 5:00PM - 7:59PM
                    }
                    if (hour >= 10)
                    {
                        return TimeOfDay.Day; // 10:00AM - 4:59PM
                    }
                    return TimeOfDay.Morning; // 5:00AM - 9:59AM
                }
                case Season.Summer:
                {
                    if (hour >= 21 || hour <= 3)
                    {
                        return TimeOfDay.Night; // 9:00PM - 3:59AM
                    }
                    if (hour >= 19)
                    {
                        return TimeOfDay.Evening; // 7:00PM - 8:59PM
                    }
                    if (hour >= 9)
                    {
                        return TimeOfDay.Day; // 9:00AM - 6:59PM
                    }
                    return TimeOfDay.Morning; // 4:00AM - 8:59AM
                }
                case Season.Autumn:
                {
                    if (hour >= 20 || hour <= 5)
                    {
                        return TimeOfDay.Night; // 8:00PM - 5:59AM
                    }
                    if (hour >= 18)
                    {
                        return TimeOfDay.Evening; // 6:00PM - 7:59PM
                    }
                    if (hour >= 10)
                    {
                        return TimeOfDay.Day; // 10:00AM - 5:59PM
                    }
                    return TimeOfDay.Morning; // 6:00AM - 9:59AM
                }
                case Season.Winter:
                {
                    if (hour >= 19 || hour <= 6)
                    {
                        return TimeOfDay.Night; // 7:00PM - 6:59AM
                    }
                    if (hour >= 17)
                    {
                        return TimeOfDay.Evening; // 5:00PM - 6:59AM
                    }
                    if (hour >= 11)
                    {
                        return TimeOfDay.Day; // 11:00AM - 4:59PM
                    }
                    return TimeOfDay.Morning; // 7:00AM - 10:59AM
                }
                default: throw new ArgumentOutOfRangeException(nameof(season));
            }
        }
    }
}
