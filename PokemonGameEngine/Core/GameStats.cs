namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class GameStats
    {
        private readonly int[] _values;

        public int this[GameStat var]
        {
            get => _values[(int)var];
            set => _values[(int)var] = value;
        }

        public GameStats()
        {
            _values = new int[(int)GameStat.MAX];
        }
    }
}
