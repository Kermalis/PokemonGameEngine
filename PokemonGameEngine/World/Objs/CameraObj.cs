using Kermalis.PokemonGameEngine.World.Maps;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    // This is really a dummy Obj class now. Could be reused for other invisible Objs with the id passed in
    internal sealed class CameraObj : Obj
    {
        public CameraObj(Map map, in WorldPos pos)
            : base(Overworld.CameraId, pos)
        {
            Map = map;
        }
    }
}
