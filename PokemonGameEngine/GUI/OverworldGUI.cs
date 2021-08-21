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
using Kermalis.PokemonGameEngine.Render.OpenGL;
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
        public static OverworldGUI Instance { get; private set; }

        private readonly TaskList _tasks = new();

        private EventObj _interactiveScriptWaitingFor;
        private string _interactiveScript;

        private FadeColorTransition _fadeTransition;

        private Window _startMenuWindow;
        private TextGUIChoices _startMenuChoices;

        private OverworldGUI()
        {
            SetupStartMenuChoices();
            Instance = this;
        }

        public static void Debug_InitOverworldGUI()
        {
            _ = new OverworldGUI(); // Create

            ProcessDayTint(true);
            LoadMapMusic();
            Instance._fadeTransition = new FadeFromColorTransition(500, Colors.Black);
            Engine.Instance.SetCallback(Instance.CB_FadeIn);
            Engine.Instance.SetRCallback(Instance.RCB_Fading);
        }

        public void SetInteractiveScript(EventObj talkedTo, string script)
        {
            Engine.Instance.Save.Vars[Var.LastTalked] = (short)talkedTo.Id; // Special var for the last person we talked to
            talkedTo.TalkedTo = true;
            PlayerObj.Player.IsWaitingForObjToStartScript = true;
            _interactiveScriptWaitingFor = talkedTo;
            _interactiveScript = script;
        }

        public static void ProcessDayTint(bool skipTransition)
        {
            if (Overworld.ShouldRenderDayTint())
            {
                DayTint.LogicTick(skipTransition);
            }
        }

        private void StartMenu_DebugBagSelected()
        {
            _fadeTransition = new FadeToColorTransition(500, Colors.Black);
            Engine.Instance.SetCallback(CB_FadeOutToBag);
            Engine.Instance.SetRCallback(RCB_Fading);
        }
        private void StartMenu_DebugPCSelected()
        {
            _fadeTransition = new FadeToColorTransition(500, Colors.Black);
            Engine.Instance.SetCallback(CB_FadeOutToPC);
            Engine.Instance.SetRCallback(RCB_Fading);
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
            _startMenuWindow = new Window(new RelPos2D(0.72f, 0.05f), s, Colors.White);
            RenderStartMenuChoicesOntoWindow();
        }
        private void RenderStartMenuChoicesOntoWindow()
        {
            _startMenuChoices.RenderChoicesOntoWindow(_startMenuWindow);
        }
        public void OpenStartMenu()
        {
            SetupStartMenuWindow();
            Engine.Instance.SetCallback(CB_StartMenu);
        }
        private void CloseStartMenuAndSetCB()
        {
            _startMenuWindow.Close();
            _startMenuWindow = null;
            Engine.Instance.SetCallback(CB_LogicTick);
        }

        public void OpenPartyMenu(PartyGUI.Mode mode)
        {
            _fadeTransition = new FadeToColorTransition(500, Colors.Black);
            switch (mode)
            {
                case PartyGUI.Mode.PkmnMenu:
                {
                    Engine.Instance.SetCallback(CB_FadeOutToParty_PkmnMenu);
                    break;
                }
                case PartyGUI.Mode.SelectDaycare:
                {
                    Engine.Instance.IsOnOverworld = false;
                    Engine.Instance.SetCallback(CB_FadeOutToParty_SelectDaycare);
                    break;
                }
                default: throw new Exception();
            }
            Engine.Instance.SetRCallback(RCB_Fading);
        }
        public void StartEggHatchScreen()
        {
            _fadeTransition = new FadeToColorTransition(500, Colors.Black);
            Engine.Instance.IsOnOverworld = false;
            Engine.Instance.SetCallback(CB_FadeOutToEggHatchScreen);
            Engine.Instance.SetRCallback(RCB_Fading);
        }
        public void StartBattle(PBEBattle battle, Song song, IReadOnlyList<Party> trainerParties, TrainerClass c = default, string defeatText = null)
        {
            Engine.Instance.IsOnOverworld = false;
            _ = new BattleGUI(battle, ReturnToFieldWithFadeInAfterEvolutionCheck, trainerParties, trainerClass: c, trainerDefeatText: defeatText);
            SoundControl.SetBattleBGM(song);
            _fadeTransition = new SpiralTransition();
            Engine.Instance.SetCallback(CB_FadeOutToBattle);
            Engine.Instance.SetRCallback(RCB_Fading);
        }
        public void TempWarp(in Warp warp)
        {
            var w = WarpInProgress.Start(warp);
            SoundControl.SetOverworldBGM(w.DestMapLoaded.Details.Music);
            _fadeTransition = new FadeToColorTransition(500, Colors.Black);
            Engine.Instance.SetCallback(CB_FadeOutToWarp);
            Engine.Instance.SetRCallback(RCB_Fading);
        }

        private void ReturnToStartMenuWithFadeIn()
        {
            ProcessDayTint(true); // Catch up time
            SetupStartMenuWindow();
            _fadeTransition = new FadeFromColorTransition(500, Colors.Black);
            Engine.Instance.SetCallback(CB_FadeInToStartMenu);
            Engine.Instance.SetRCallback(RCB_Fading);
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
        public void ReturnToFieldWithFadeIn()
        {
            _fadeTransition = new FadeFromColorTransition(500, Colors.Black);
            Engine.Instance.SetCallback(CB_FadeIn);
            Engine.Instance.SetRCallback(RCB_Fading);
        }

        private static void LoadMapMusic()
        {
            SoundControl.SetOverworldBGM(PlayerObj.Player.Map.Details.Music);
        }

        private void CB_FadeIn()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Engine.Instance.IsOnOverworld = true;
                Engine.Instance.SetCallback(CB_LogicTick);
                Engine.Instance.SetRCallback(RCB_RenderOverworld);
            }
        }
        private void CB_FadeInToStartMenu()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Engine.Instance.IsOnOverworld = true;
                Engine.Instance.SetCallback(CB_StartMenu);
                Engine.Instance.SetRCallback(RCB_RenderOverworld);
            }
        }
        private void CB_FadeOutToWarp()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Obj player = PlayerObj.Player;
                player.Warp();
                if (player.QueuedScriptMovements.Count > 0)
                {
                    player.RunNextScriptMovement();
                    player.IsScriptMoving = true;
                }
                ProcessDayTint(true); // Catch up time
                _fadeTransition = new FadeFromColorTransition(500, Colors.Black);
                Engine.Instance.SetCallback(CB_FadeIn);
                Engine.Instance.SetRCallback(RCB_Fading);
            }
        }
        private void CB_FadeOutToEggHatchScreen()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _ = new EggHatchGUI();
            }
        }
        private void CB_FadeOutToParty_PkmnMenu()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _startMenuWindow.Close();
                _startMenuWindow = null;
                _ = new PartyGUI(Engine.Instance.Save.PlayerParty, PartyGUI.Mode.PkmnMenu, ReturnToStartMenuWithFadeIn);
            }
        }
        private void CB_FadeOutToParty_SelectDaycare()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _ = new PartyGUI(Engine.Instance.Save.PlayerParty, PartyGUI.Mode.SelectDaycare, ReturnToFieldWithFadeIn);
            }
        }
        private void CB_FadeOutToBag()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _startMenuWindow.Close();
                _startMenuWindow = null;
                _ = new BagGUI(Engine.Instance.Save.PlayerInventory, Engine.Instance.Save.PlayerParty, ReturnToStartMenuWithFadeIn);
            }
        }
        private void CB_FadeOutToPC()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _startMenuWindow.Close();
                _startMenuWindow = null;
                _ = new PCBoxesGUI(Engine.Instance.Save.PCBoxes, Engine.Instance.Save.PlayerParty, ReturnToStartMenuWithFadeIn);
            }
        }
        private void CB_FadeOutToBattle()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                BattleGUI.Instance.FadeIn();
            }
        }
        private void CB_LogicTick()
        {
            ScriptContext.ProcessAll();
            StringPrinter.ProcessAll();
            Tileset.AnimationTick();
            ProcessDayTint(false);
            _tasks.RunTasks();

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
                    o.LogicTick();
                }
            }

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
        private void CB_StartMenu()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            int s = _startMenuChoices.Selected;
            _startMenuChoices.HandleInputs();
            if (_startMenuWindow is null)
            {
                return; // Was just closed
            }
            if (s != _startMenuChoices.Selected)
            {
                RenderStartMenuChoicesOntoWindow();
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
            _fadeTransition = new FadeFromColorTransition(500, Colors.Black);
            Engine.Instance.SetCallback(CB_FadeInToUseSurf);
            Engine.Instance.SetRCallback(RCB_Fading);
        }
        private void CB_FadeInToUseSurf()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Engine.Instance.IsOnOverworld = true;
                StartSurfTasks();
                Engine.Instance.SetCallback(CB_LogicTick);
                Engine.Instance.SetRCallback(RCB_RenderOverworld);
            }
        }

        public void StartSurfTasks()
        {
            PartyPokemon pkmn = Engine.Instance.Save.PlayerParty[Engine.Instance.Save.Vars[Var.SpecialVar_Result]];
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
            if ((bool)task.Data)
            {
                PlayerObj player = PlayerObj.Player;
                player.State = PlayerObjState.Surfing;
                player.QueuedScriptMovements.Enqueue(Obj.GetWalkMovement(player.Facing));
                player.RunNextScriptMovement();
                player.IsScriptMoving = true;
                CameraObj.CopyMovementIfAttachedTo(player);
                task.Action = Task_Surf_WaitMovement;
            }
        }
        private void Task_Surf_WaitMovement(BackTask task)
        {
            if (PlayerObj.Player.IsMoving)
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

        private void RCB_Fading(GL gl)
        {
            RCB_RenderOverworld(gl);
            _fadeTransition.Render(gl);
        }
        private void RCB_RenderOverworld(GL gl)
        {
            GLHelper.ClearColor(gl, Colors.Black);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            MapRenderer.Render(Game.RenderSize);
            /*if (Overworld.ShouldRenderDayTint())
            {
                DayTint.Render();
            }*/
            Window.RenderAll();
        }
    }
}
