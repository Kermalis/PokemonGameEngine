namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal class CameraObj : Obj
    {
        public static readonly CameraObj Camera = new CameraObj();
        public static int CameraOfsX;
        public static int CameraOfsY;
        public static Obj CameraAttachedTo = PlayerObj.Player;

        private CameraObj()
            : base(Overworld.CameraId)
        {
        }

        public static void CameraCopyMovement()
        {
            Obj c = Camera;
            Obj other = CameraAttachedTo;
            c.CopyMovement(other);
        }

        protected override void UpdateMap(Map newMap)
        {
            Map curMap = Map;
            if (curMap != newMap)
            {
                curMap.UnloadObjEvents();
                newMap.LoadObjEvents();
                newMap.Objs.Add(this);
                Map = newMap;
            }
        }
    }
}
