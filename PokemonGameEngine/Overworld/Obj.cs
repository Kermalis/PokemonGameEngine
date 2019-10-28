namespace Kermalis.PokemonGameEngine.Overworld
{
    internal class Obj
    {
        public const ushort PlayerId = ushort.MaxValue;
        public const ushort CameraId = PlayerId - 1;

        public static Obj Camera = new Obj(CameraId);

        public readonly ushort Id;

        public int X;
        public int Y;

        protected Obj(ushort id)
        {
            Id = id;
        }
    }
}
