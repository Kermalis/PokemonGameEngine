using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class OverworldGUI
    {
        public void LogicTick()
        {
            List<Obj> list = Obj.LoadedObjs;
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                list[i].UpdateMovementTimer();
            }
            for (int i = 0; i < count; i++)
            {
                Obj o = list[i];
                if (o != CameraObj.CameraAttachedTo)
                {
                    o.LogicTick();
                }
            }
            CameraObj.CameraAttachedTo?.LogicTick(); // This obj should logic tick last so map changing doesn't change LoadedObjs
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.FillColor(bmpAddress, bmpWidth, bmpHeight, 0xFF000000);
            CameraObj.Render(bmpAddress, bmpWidth, bmpHeight);
            if (Overworld.ShouldRenderDayTint())
            {
                DayTint.Render(bmpAddress, bmpWidth, bmpHeight);
            }
        }
    }
}
