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
            _frameBuffer.Use();
            _startMenuWindow?.Close(); // Possibly activated this from the PartyGUI
            _startMenuWindow = null;
            foreach (Obj o in Obj.LoadedObjs)
            {
                o.IsLocked = true;
            }

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInToUseSurf);
        }

        public void StartSurfTasks()
        {
            PartyPokemon pkmn = Game.Instance.Save.PlayerParty[Game.Instance.Save.Vars[Var.SpecialVar_Result]];
            _tasks.Add(Task_Surf_PlayCry, int.MaxValue, data: pkmn);
            // TODO: Clear saved music, start surf music
        }
        private void Task_Surf_PlayCry(BackTask task)
        {
            void OnCryFinished(SoundChannel _)
            {
                task.Data = true;
            }

            var pkmn = (PartyPokemon)task.Data;
            SoundControl.PlayCry(pkmn.Species, pkmn.Form, onStopped: OnCryFinished);
            task.Data = false;
            task.Action = Task_Surf_WaitCry;
        }
        private void Task_Surf_WaitCry(BackTask task)
        {
            if (!(bool)task.Data)
            {
                return; // Gets set to true when the cry ends
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

            _tasks.RemoveAndDispose(task);
            foreach (Obj o in Obj.LoadedObjs)
            {
                o.IsLocked = false;
            }
        }

        private void CB_FadeInToUseSurf()
        {
            Render();
            _transition.Render();
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
