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
                Obj o = list[i];
                // Do not move locked Objs unless they're being moved by scripts
                if (!o.IsLocked || o.IsScriptMoving)
                {
                    list[i].UpdateMovement();
                }
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
            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(0x00, 0x00, 0x00, 0xFF));
            CameraObj.Render(bmpAddress, bmpWidth, bmpHeight);
            if (Overworld.ShouldRenderDayTint())
            {
                DayTint.Render(bmpAddress, bmpWidth, bmpHeight);
            }
        }
    }
}
