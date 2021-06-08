using System.Text.RegularExpressions;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class StringBuffers
    {
        private const int NumBuffers = 5; // 5 buffers, can change to however many you want
        private static readonly Regex _regex = new(@"\{BUF\s(\d+)\}");

        public readonly string[] Buffers = new string[NumBuffers];

        public StringBuffers()
        {
            for (int i = 0; i < NumBuffers; i++)
            {
                Buffers[i] = string.Empty;
            }
        }

        private string MatchEvaluator(Match m)
        {
            return Buffers[int.Parse(m.Groups[1].Value)];
        }
        public string ApplyBuffers(string input)
        {
            return _regex.Replace(input, MatchEvaluator);
        }
    }
}
