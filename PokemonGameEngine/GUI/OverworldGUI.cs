using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Pkmn;
using Kermalis.PokemonGameEngine.GUI.Player;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.World;
using Kermalis.PokemonGameEngine.Script;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.Trainer;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class OverworldGUI
    {
        public static OverworldGUI Instance { get; private set; } = null!; // Set in constructor

        private readonly TaskList _tasks = new();

        private EventObj _interactiveScriptWaitingFor;
        private string _interactiveScript;

        private FadeColorTransition _fadeTransition;

        private Window _startMenuWindow;
        private TextGUIChoices _startMenuChoices;

        private OverworldGUI()
        {
            Instance = this;

            SetupStartMenuChoices();
            DayTint.SetTintTime();
        }

        public static void Debug_InitOverworldGUI()
        {
            _ = new OverworldGUI(); // Create

            UpdateDayTint(true);
            LoadMapMusic();
            Instance._fadeTransition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(Instance.CB_FadeIn);
        }

        public void SetInteractiveScript(EventObj talkedTo, string script)
        {
            Game.Instance.Save.Vars[Var.LastTalked] = (short)talkedTo.Id; // Special var for the last person we talked to
            talkedTo.TalkedTo = true;
            PlayerObj.Instance.IsWaitingForObjToStartScript = true;
            _interactiveScriptWaitingFor = talkedTo;
            _interactiveScript = script;
        }

        public static void UpdateDayTint(bool skipTransition)
        {
            if (Overworld.ShouldRenderDayTint())
            {
                DayTint.Update(skipTransition);
            }
        }

        private void StartMenu_DebugBagSelected()
        {
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutToBag);
        }
        private void StartMenu_DebugPCSelected()
        {
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutToPC);
        }
        private void SetupStartMenuChoices()
        {
            _startMenuChoices = new TextGUIChoices(0, 0, backCommand: CloseStartMenuAndSetCB, font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            _startMenuChoices.AddOne("Pokémon", () => OpenPartyMenu(PartyGUI.Mode.PkmnMenu));
            _startMenuChoices.AddOne("Bag", StartMenu_DebugBagSelected);
            _startMenuChoices.AddOne("PC", StartMenu_DebugPCSelected);
            _startMenuChoices.AddOne("Close", CloseStartMenuAndSetCB);
        }

        private void SetupStartMenuWindow()
        {
            Size2D s = _startMenuChoices.GetSize();
            _startMenuWindow = new Window(new RelPos2D(0.72f, 0.05f), s, Colors.White4);
            RenderStartMenuChoicesOntoWindow();
        }
        private void RenderStartMenuChoicesOntoWindow()
        {
            _startMenuChoices.RenderChoicesOntoWindow(_startMenuWindow);
        }
        public void OpenStartMenu()
        {
            SetupStartMenuWindow();
            Game.Instance.SetCallback(CB_StartMenu);
        }
        private void CloseStartMenuAndSetCB()
        {
            _startMenuWindow.Close();
            _startMenuWindow = null;
            Game.Instance.SetCallback(CB_ProcessScriptsTasksAndObjs);
        }

        public void OpenPartyMenu(PartyGUI.Mode mode)
        {
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            switch (mode)
            {
                case PartyGUI.Mode.PkmnMenu:
                {
                    Game.Instance.SetCallback(CB_FadeOutToParty_PkmnMenu);
                    break;
                }
                case PartyGUI.Mode.SelectDaycare:
                {
                    Game.Instance.IsOnOverworld = false;
                    Game.Instance.SetCallback(CB_FadeOutToParty_SelectDaycare);
                    break;
                }
                default: throw new Exception();
            }
        }
        public void StartEggHatchScreen()
        {
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.IsOnOverworld = false;
            Game.Instance.SetCallback(CB_FadeOutToEggHatchScreen);
        }
        /// <summary>Sets up the battle transition, inits the battle gui, starts music, sets transition callbacks.</summary>
        public void SetupBattle(PBEBattle battle, Song song, IReadOnlyList<Party> trainerParties, TrainerClass c = default, string defeatText = null)
        {
            Game.Instance.IsOnOverworld = false;
            _ = new BattleGUI(battle, ReturnToFieldWithFadeInAfterEvolutionCheck, trainerParties, trainerClass: c, trainerDefeatText: defeatText);
            SoundControl.SetBattleBGM(song);
            _fadeTransition = new SpiralTransition();
            Game.Instance.SetCallback(CB_FadeOutToBattle);
        }
        public void TempWarp(in Warp warp)
        {
            var w = WarpInProgress.Start(warp);
            SoundControl.SetOverworldBGM(w.DestMapLoaded.Details.Music);
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutToWarp);
        }

        private void ReturnToStartMenuWithFadeIn()
        {
            UpdateDayTint(true); // Catch up time
            SetupStartMenuWindow();
            _fadeTransition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInToStartMenu);
        }
        public void ReturnToFieldWithFadeInAfterEvolutionCheck()
        {
            if (Evolution.GetNextPendingEvolution(out (PartyPokemon, EvolutionData.EvoData) pending))
            {
                (PartyPokemon pkmn, EvolutionData.EvoData evo) = pending;
                _ = new EvolutionGUI(pkmn, evo);
                return;
            }
            ReturnToFieldWithFadeIn();
        }
        /// <summary>Starts a fade from black fade and sets the callback to fade in</summary>
        public void ReturnToFieldWithFadeIn()
        {
            _fadeTransition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeIn);
        }

        private static void LoadMapMusic()
        {
            SoundControl.SetOverworldBGM(PlayerObj.Instance.Map.Details.Music);
        }

        private void CB_FadeIn()
        {
            UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            Game.Instance.IsOnOverworld = true;
            Game.Instance.SetCallback(CB_ProcessScriptsTasksAndObjs);
        }
        private void CB_FadeInToStartMenu()
        {
            UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            Game.Instance.IsOnOverworld = true;
            Game.Instance.SetCallback(CB_StartMenu);
        }
        private void CB_FadeOutToWarp()
        {
            UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            Obj player = PlayerObj.Instance;
            player.Warp();
            if (player.QueuedScriptMovements.Count > 0)
            {
                player.RunNextScriptMovement();
                player.IsScriptMoving = true;
            }
            UpdateDayTint(true); // Catch up time
            _fadeTransition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeIn);
        }
        private void CB_FadeOutToEggHatchScreen()
        {
            UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _ = new EggHatchGUI();
        }
        private void CB_FadeOutToParty_PkmnMenu()
        {
            UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _startMenuWindow.Close();
            _startMenuWindow = null;
            _ = new PartyGUI(Game.Instance.Save.PlayerParty, PartyGUI.Mode.PkmnMenu, ReturnToStartMenuWithFadeIn);
        }
        private void CB_FadeOutToParty_SelectDaycare()
        {
            UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _ = new PartyGUI(Game.Instance.Save.PlayerParty, PartyGUI.Mode.SelectDaycare, ReturnToFieldWithFadeIn);
        }
        private void CB_FadeOutToBag()
        {
            UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _startMenuWindow.Close();
            _startMenuWindow = null;
            _ = new BagGUI(Game.Instance.Save.PlayerInventory, Game.Instance.Save.PlayerParty, ReturnToStartMenuWithFadeIn);
        }
        private void CB_FadeOutToPC()
        {
            UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _startMenuWindow.Close();
            _startMenuWindow = null;
            _ = new PCBoxesGUI(Game.Instance.Save.PCBoxes, Game.Instance.Save.PlayerParty, ReturnToStartMenuWithFadeIn);
        }
        private void CB_FadeOutToBattle()
        {
            UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            BattleGUI.Instance.InitFadeIn();
        }
        private void CB_ProcessScriptsTasksAndObjs()
        {
            ScriptContext.UpdateAll();
            StringPrinter.UpdateAll();
            UpdateDayTint(false);
            _tasks.RunTasks();
            ProcessObjs();

            Render();
        }
        private void CB_StartMenu()
        {
            UpdateDayTint(false);
            int s = _startMenuChoices.Selected;
            _startMenuChoices.HandleInputs();
            // Check if the window was just closed
            if (_startMenuWindow is not null && s != _startMenuChoices.Selected)
            {
                RenderStartMenuChoicesOntoWindow(); // Update selection if it has changed
            }

            Render();
        }

        private void ProcessObjs()
        {
            // We can eliminate the need for array alloc if we have Next and Prev like tasks
            Obj[] arr = Obj.LoadedObjs.ToArray();
            for (int i = 0; i < arr.Length; i++)
            {
                Obj o = arr[i];
                if (Obj.LoadedObjs.Contains(o) && o.ShouldUpdateMovement)
                {
                    o.UpdateMovement();
                }
            }
            arr = Obj.LoadedObjs.ToArray();
            for (int i = 0; i < arr.Length; i++)
            {
                Obj o = arr[i];
                if (Obj.LoadedObjs.Contains(o))
                {
                    o.Update();
                }
            }

            // Check for the obj we're waiting for to finish moving
            if (_interactiveScriptWaitingFor?.IsMoving == false)
            {
                EventObj o = _interactiveScriptWaitingFor;
                _interactiveScriptWaitingFor = null;
                string script = _interactiveScript;
                _interactiveScript = null;
                o.TalkedTo = false;
                PlayerObj.Instance.IsWaitingForObjToStartScript = false;
                ScriptLoader.LoadScript(script);
            }
        }

        #region Surf

        public void ReturnToFieldAndUseSurf()
        {
            _startMenuWindow?.Close();
            _startMenuWindow = null;
            foreach (Obj o in Obj.LoadedObjs)
            {
                o.IsLocked = true;
            }

            _fadeTransition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInToUseSurf);
        }
        private void CB_FadeInToUseSurf()
        {
            UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            Game.Instance.IsOnOverworld = true;
            StartSurfTasks();
            Game.Instance.SetCallback(CB_ProcessScriptsTasksAndObjs);
        }

        public void StartSurfTasks()
        {
            PartyPokemon pkmn = Game.Instance.Save.PlayerParty[Game.Instance.Save.Vars[Var.SpecialVar_Result]];
            _tasks.Add(Task_SurfInit, int.MaxValue, data: pkmn);
            // TODO: Clear saved music, start surf music
        }
        private void Task_SurfInit(BackTask task)
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
            CameraObj.CopyMovementIfAttachedTo(player); // Tell camera to move the same way
            task.Action = Task_Surf_WaitMovement;
        }
        private void Task_Surf_WaitMovement(BackTask task)
        {
            if (PlayerObj.Instance.IsMoving)
            {
                return;
            }

            foreach (Obj o in Obj.LoadedObjs)
            {
                o.IsLocked = false;
            }
            _tasks.RemoveAndDispose(task);
        }

        #endregion

        private void RenderFading()
        {
            Render();
            _fadeTransition.Render();
        }
        private static void Render()
        {
            GL gl = Display.OpenGL;
            gl.ClearColor(Colors.Black3);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            MapRenderer.Render();
            if (Overworld.ShouldRenderDayTint())
            {
                DayTint.Render();
            }
            Window.RenderAll();
        }
    }
}
