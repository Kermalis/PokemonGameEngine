namespace Kermalis.PokemonGameEngine.Sound
{
    internal sealed record SoundChannelState(string Asset, float Panpot, int Priority, float Freq, float InterPos, long Offset, long TrailOffset)
    {
    }
}
