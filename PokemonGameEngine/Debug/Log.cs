#if DEBUG
using System;

namespace Kermalis.PokemonGameEngine.Debug
{
    internal static class Log
    {
        private const char IndentChar = '-';
        private static int _indentLevel = 0;
        private static string _indent = string.Empty;

        public static void ModifyIndent(int levels)
        {
            if (levels == 0)
            {
                return;
            }
            _indentLevel += levels;
            _indent = string.Empty.PadLeft(_indentLevel, IndentChar);
        }

        public static void WriteLine(string str)
        {
            if (_indentLevel != 0)
            {
                str = _indent + ' ' + str;
            }
            Console.WriteLine(str);
        }
        public static void WriteLineWithTime(string str)
        {
            WriteLine(string.Format("({0}) -- {1}", DateTime.Now.ToLongTimeString(), str));
        }
    }
}
#endif
