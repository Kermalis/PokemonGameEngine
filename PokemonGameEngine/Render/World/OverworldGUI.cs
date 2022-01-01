using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render.Battle;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Pkmn;
using Kermalis.PokemonGameEngine.Render.Player;
using Kermalis.PokemonGameEngine.Render.Transitions;
using Kermalis.PokemonGameEngine.Script;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.Trainer;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed class OverworldGUI
    {
        public static OverworldGUI Instance { get; private set; } = null!; // Set in constructor

        // A block is 16x16 pixels (2x2 tiles, and a tile is 8x8 pixels)
        // You can have different sized blocks and tiles if you wish, but this table is demonstrating defaults
        // GB/GBC        - 160 x 144 resolution (10:9) - 10 x  9   blocks
        // GBA           - 240 x 160 resolution ( 3:2) - 15 x 10   blocks
        // NDS           - 256 x 192 resolution ( 4:3) - 16 x 12   blocks
        // 3DS (Lower)   - 320 x 240 resolution ( 4:3) - 20 x 15   blocks
        // 3DS (Upper)   - 400 x 240 resolution ( 5:3) - 25 x 15   blocks
        // Default below - 384 x 216 resolution (16:9) - 24 x 13.5 blocks
        public static readonly Size2D RenderSize = new(384, 216);

        private readonly FrameBuffer _frameBuffer;
        private readonly FrameBuffer _dayTintFrameBuffer;
        private readonly TaskList _tasks = new();

        private EventObj _interactiveScriptWaitingFor;
        private string _interactiveScript;

        private ITransition _transition;

        private Window _startMenuWindow;
        private TextGUIChoices _startMenuChoices;

        private OverworldGUI()
        {
            Instance = this;

            _frameBuffer = FrameBuffer.CreateWithColorAndDepth(RenderSize);
            _frameBuffer.Use();
            _dayTintFrameBuffer = FrameBuffer.CreateWithColor(RenderSize);
            _ = new MapRenderer(); // Init
            SetupStartMenuChoices();
            DayTint.SetTintTime();
        }

        public static void Debug_InitOverworldGUI()
        {
            _ = new OverworldGUI(); // Create

            DayTint.CatchUpTime = true;
            LoadMapMusic();
            Instance._transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(Instance.CB_FadeIn);
        }
        public static void Debug_InitTestBattle()
        {
            _ = new OverworldGUI(); // Create

            DayTint.CatchUpTime = true;
            LoadMapMusic();
            //EncounterMaker.Debug_CreateTestWildBattle();
            TrainerCore.Debug_CreateTestTrainerBattle();
        }

        public void SetInteractiveScript(EventObj talkedTo, string script)
        {
            Game.Instance.Save.Vars[Var.LastTalked] = (short)talkedTo.Id; // Special var for the last person we talked to
            talkedTo.TalkedTo = true;
            PlayerObj.Instance.IsWaitingForObjToStartScript = true;
            _interactiveScriptWaitingFor = talkedTo;
            _interactiveScript = script;
        }

        private void StartMenu_DebugBagSelected()
        {
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutToBag);
        }
        private void StartMenu_DebugPCSelected()
        {
            _transition = FadeToColorTransition.ToBlackStandard();
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
            _startMenuWindow = new Window(Pos2D.FromRelative(0.72f, 0.05f, RenderSize), s, Colors.White4);
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
            _transition = FadeToColorTransition.ToBlackStandard();
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
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.IsOnOverworld = false;
            Game.Instance.SetCallback(CB_FadeOutToEggHatchScreen);
        }
        public void StartWildBattle(PBEBattle battle, Song song, IReadOnlyList<Party> trainerParties)
        {
            BattleGUI.CreateWildBattle(battle, ReturnToFieldWithFadeInAfterEvolutionCheck, trainerParties);
            StartBattle(song);
        }
        public void StartTrainerBattle(PBEBattle battle, Song song, IReadOnlyList<Party> trainerParties, TrainerClass c, string defeatText)
        {
            BattleGUI.CreateTrainerBattle(battle, ReturnToFieldWithFadeInAfterEvolutionCheck, trainerParties, c, defeatText);
            StartBattle(song);
        }
        /// <summary>Sets up the battle transition, starts music, sets transition callbacks.</summary>
        private void StartBattle(Song song)
        {
            Game.Instance.IsOnOverworld = false;
            SoundControl.SetBattleBGM(song);
            _transition = new BattleTransition_Liquid();
            Game.Instance.SetCallback(CB_FadeOutToBattle);
        }
        public void TempWarp(in Warp warp)
        {
            var w = WarpInProgress.Start(warp);
            SoundControl.SetOverworldBGM(w.DestMapLoaded.Details.Music);
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutToWarp);
        }

        private void ReturnToStartMenuWithFadeIn()
        {
            _frameBuffer.Use();
            DayTint.CatchUpTime = true;
            SetupStartMenuWindow();
            _transition = FadeFromColorTransition.FromBlackStandard();
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
        /// <summary>Sets the OverworldGUI's fbo, starts a fade from black fade, and sets the callback to fade in</summary>
        public void ReturnToFieldWithFadeIn()
        {
            _frameBuffer.Use();
            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeIn);
        }

        private static void LoadMapMusic()
        {
            SoundControl.SetOverworldBGM(PlayerObj.Instance.Map.Details.Music);
        }

        private void CB_FadeIn()
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
            Game.Instance.SetCallback(CB_ProcessScriptsTasksAndObjs);
        }
        private void CB_FadeInToStartMenu()
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
            Game.Instance.SetCallback(CB_StartMenu);
        }
        private void CB_FadeOutToWarp()
        {
            Render();
            _transition.Render();
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            DayTint.CatchUpTime = true;
            Obj player = PlayerObj.Instance;
            player.Warp();
            if (player.QueuedScriptMovements.Count > 0)
            {
                player.RunNextScriptMovement();
                player.IsScriptMoving = true;
            }
            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeIn);
        }
        private void CB_FadeOutToEggHatchScreen()
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
            _ = new EggHatchGUI();
        }
        private void CB_FadeOutToParty_PkmnMenu()
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
            _startMenuWindow.Close();
            _startMenuWindow = null;
            _ = new PartyGUI(Game.Instance.Save.PlayerParty, PartyGUI.Mode.PkmnMenu, ReturnToStartMenuWithFadeIn);
        }
        private void CB_FadeOutToParty_SelectDaycare()
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
            _ = new PartyGUI(Game.Instance.Save.PlayerParty, PartyGUI.Mode.SelectDaycare, ReturnToFieldWithFadeIn);
        }
        private void CB_FadeOutToBag()
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
            _startMenuWindow.Close();
            _startMenuWindow = null;
            _ = new BagGUI(Game.Instance.Save.PlayerInventory, Game.Instance.Save.PlayerParty, ReturnToStartMenuWithFadeIn);
        }
        private void CB_FadeOutToPC()
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
            _startMenuWindow.Close();
            _startMenuWindow = null;
            _ = new PCBoxesGUI(Game.Instance.Save.PCBoxes, Game.Instance.Save.PlayerParty, ReturnToStartMenuWithFadeIn);
        }
        private void CB_FadeOutToBattle()
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
            BattleGUI.Instance.InitFadeIn();
        }
        private void CB_ProcessScriptsTasksAndObjs()
        {
            ScriptContext.UpdateAll();
            StringPrinter.UpdateAll();
            _tasks.RunTasks();
            ProcessObjs();

            Render();
            _frameBuffer.BlitToScreen();
#if DEBUG_OVERWORLD
            MapRenderer.Instance.Debug_RenderBlocks();
#endif
        }
        private void CB_StartMenu()
        {
            int s = _startMenuChoices.Selected;
            _startMenuChoices.HandleInputs();
            // Check if the window was just closed
            if (_startMenuWindow is not null && s != _startMenuChoices.Selected)
            {
                RenderStartMenuChoicesOntoWindow(); // Update selection if it has changed
            }

            Render();
            _frameBuffer.BlitToScreen();
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
            _frameBuffer.Use();
            _startMenuWindow?.Close();
            _startMenuWindow = null;
            foreach (Obj o in Obj.LoadedObjs)
            {
                o.IsLocked = true;
            }

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInToUseSurf);
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

        #endregion

        private void Render()
        {
            GL gl = Display.OpenGL;
            gl.ClearColor(Colors.Black3);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            MapRenderer.Instance.Render();
            DayTint.Render(_dayTintFrameBuffer);
            Window.RenderAll();
        }
    }
}
