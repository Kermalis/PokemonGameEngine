namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal sealed class PlayerObj : VisualObj
    {
        public static readonly PlayerObj Player = new PlayerObj();

        private PlayerObj()
            : base(Overworld.PlayerId, "Player")
        {
        }
    }
}
