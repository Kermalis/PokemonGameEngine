using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.Transitions;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.World.Objs;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed partial class OverworldGUI
    {
        public void ReturnToFieldAndUseSurf()
        {
            _startMenuWindow?.Close(); // Possibly activated this from the PartyGUI
            _startMenuWindow = null;
            for (Obj o = Obj.LoadedObjs.First; o is not null; o = o.Next)
            {
                o.IsLocked = true;
            }

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInToUseSurf);
        }

        public void StartSurfTasks()
        {
            PartyPokemon pkmn = Game.Instance.Save.PlayerParty[Game.Instance.Save.Vars[Var.SpecialVar_Result]];
            SoundChannel channel = SoundControl.PlayCry(pkmn.Species, pkmn.Form);
            _tasks.Add(new BackTask(Task_Surf_WaitCry, int.MaxValue, data: channel));
            // TODO: Clear saved music, start surf music
        }
        private void Task_Surf_WaitCry(BackTask task)
        {
            var channel = (SoundChannel)task.Data;
            if (!channel.IsStopped)
            {
                return;
            }

            PlayerObj player = PlayerObj.Instance;
            player.State = PlayerObjState.Surfing;
            player.QueuedScriptMovements.Enqueue(Obj.GetWalkMovement(player.Facing));
            player.RunNextScriptMovement();
            player.IsScriptMoving = true;
            CameraObj.Instance.CopyMovementIfAttachedTo(player); // Tell camera to move the same way
            task.Action = Task_Surf_WaitMovement;
        }
        private void Task_Surf_WaitMovement(BackTask task)
        {
            if (PlayerObj.Instance.IsMoving)
            {
                return;
            }

            _tasks.Remove(task);
            for (Obj o = Obj.LoadedObjs.First; o is not null; o = o.Next)
            {
                o.IsLocked = false;
            }
        }

        private void CB_FadeInToUseSurf()
        {
            Render();
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            Game.Instance.IsOnOverworld = true;
            StartSurfTasks();
            Game.Instance.SetCallback(CB_ProcessScriptsTasksAndObjs);
        }
    }
}
