using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Maps;
using Kermalis.PokemonGameEngine.World.Objs;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed partial class OverworldGUI
    {
        public Obj CamAttachedTo;
        public Vec2I CamVisualOfs;

        public void InitCamera(Obj obj)
        {
            CamAttachedTo = obj;
            obj.Map.OnCurrentMap();
            Overworld.UpdateDayTintEnabled();
        }

        public void SetCamAttachment_HandleMapChange(Obj newObj)
        {
            Map oldMap = CamAttachedTo.Map;
            CamAttachedTo = newObj;
            if (oldMap != newObj.Map)
            {
                Overworld.OnCameraMapChanged(oldMap, newObj.Map);
            }
        }
    }
}
