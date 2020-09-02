using Kermalis.PokemonGameEngine.Scripts;
using System;

namespace Kermalis.PokemonGameEngine.Script
{
    internal static class ScriptUtils
    {
        public static bool Match(this ScriptConditional cond, long value1, long value2)
        {
            switch (cond)
            {
                case ScriptConditional.Equal: return value1 == value2;
                case ScriptConditional.GreaterEqual: return value1 >= value2;
                case ScriptConditional.LessEqual: return value1 <= value2;
                case ScriptConditional.NotEqual: return value1 != value2;
                case ScriptConditional.Greater: return value1 > value2;
                case ScriptConditional.Less: return value1 < value2;
                default: throw new ArgumentOutOfRangeException(nameof(cond));
            }
        }
    }
}
