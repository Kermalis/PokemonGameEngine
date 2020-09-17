using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Script;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class OverworldGUI
    {
        private EventObj _interactiveScriptWaitingFor;
        private string _interactiveScript;

        public void SetInteractiveScript(EventObj talkedTo, string script)
        {
            Game.Instance.Save.Vars[Var.LastTalked] = (short)talkedTo.Id; // Special var for the last person we talked to
            talkedTo.TalkedTo = true;
            PlayerObj.Player.IsWaitingForObjToStartScript = true;
            _interactiveScriptWaitingFor = talkedTo;
            _interactiveScript = script;
        }

        public void LogicTick()
        {
            List<Obj> list = Obj.LoadedObjs;
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                Obj o = list[i];
                if (o.ShouldUpdateMovement)
                {
                    o.UpdateMovement();
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

            if (_interactiveScriptWaitingFor?.IsMoving == false)
            {
                EventObj o = _interactiveScriptWaitingFor;
                _interactiveScriptWaitingFor = null;
                string script = _interactiveScript;
                _interactiveScript = null;
                o.TalkedTo = false;
                PlayerObj.Player.IsWaitingForObjToStartScript = false;
                ScriptLoader.LoadScript(script);
            }
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.OverwriteRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(0, 0, 0, 255));
            CameraObj.Render(bmpAddress, bmpWidth, bmpHeight);
            if (Overworld.ShouldRenderDayTint())
            {
                DayTint.Render(bmpAddress, bmpWidth, bmpHeight);
            }
        }
    }
}
