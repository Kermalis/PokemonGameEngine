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
using Kermalis.PokemonGameEngine.Script;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;
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
        private IWarp _warpingTo;

        private Window _startMenuWindow;
        private TextGUIChoices _startMenuChoices;

        private OverworldGUI()
        {
            SetupStartMenuChoices();
            Instance = this;
        }

        public static unsafe void Debug_InitOverworldGUI()
        {
            _ = new OverworldGUI(); // Create

            ProcessDayTint(true);
            LoadMapMusic();
            Instance._fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(Instance.CB_FadeIn);
            Game.Instance.SetRCallback(Instance.RCB_Fading);
        }

        public void SetInteractiveScript(EventObj talkedTo, string script)
        {
            Game.Instance.Save.Vars[Var.LastTalked] = (short)talkedTo.Id; // Special var for the last person we talked to
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

        private unsafe void StartMenu_DebugBagSelected()
        {
            _fadeTransition = new FadeToColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeOutToBag);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        private unsafe void StartMenu_DebugPCSelected()
        {
            _fadeTransition = new FadeToColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeOutToPC);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        private void SetupStartMenuChoices()
        {
            _startMenuChoices = new TextGUIChoices(0, 0, backCommand: CloseStartMenuAndSetCB, font: Font.Default, fontColors: Font.DefaultDarkGray_I, selectedColors: Font.DefaultYellow_O);
            _startMenuChoices.Add(new TextGUIChoice("Pokémon", () => OpenPartyMenu(PartyGUI.Mode.PkmnMenu)));
            _startMenuChoices.Add(new TextGUIChoice("Bag", StartMenu_DebugBagSelected));
            _startMenuChoices.Add(new TextGUIChoice("PC", StartMenu_DebugPCSelected));
            _startMenuChoices.Add(new TextGUIChoice("Close", CloseStartMenuAndSetCB));
        }

        private void SetupStartMenuWindow()
        {
            _startMenuChoices.GetSize(out int width, out int height);
            _startMenuWindow = new Window(0.72f, 0.05f, width, height, RenderUtils.Color(255, 255, 255, 255));
            RenderStartMenuChoicesOntoWindow();
        }
        private unsafe void RenderStartMenuChoicesOntoWindow()
        {
            _startMenuWindow.ClearImage();
            Image i = _startMenuWindow.Image;
            fixed (uint* bmpAddress = i.Bitmap)
            {
                _startMenuChoices.Render(bmpAddress, i.Width, i.Height);
            }
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
            Game.Instance.SetCallback(CB_LogicTick);
        }

        public unsafe void OpenPartyMenu(PartyGUI.Mode mode)
        {
            _fadeTransition = new FadeToColorTransition(500, 0);
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
            Game.Instance.SetRCallback(RCB_Fading);
        }
        public unsafe void StartEggHatchScreen()
        {
            _fadeTransition = new FadeToColorTransition(500, 0);
            Game.Instance.IsOnOverworld = false;
            Game.Instance.SetCallback(CB_FadeOutToEggHatchScreen);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        public unsafe void StartBattle(PBEBattle battle, Song song, IReadOnlyList<Party> trainerParties)
        {
            Game.Instance.IsOnOverworld = false;
            _ = new BattleGUI(battle, ReturnToFieldWithFadeInAfterEvolutionCheck, trainerParties);
            SoundControl.SetBattleBGM(song);
            _fadeTransition = new SpiralTransition();
            Game.Instance.SetCallback(CB_FadeOutToBattle);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        public unsafe void TempWarp(IWarp warp)
        {
            _warpingTo = warp;
            SoundControl.SetOverworldBGM(Map.LoadOrGet(warp.DestMapId).MapDetails.Music);
            _fadeTransition = new FadeToColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeOutToWarp);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void ReturnToStartMenuWithFadeIn()
        {
            ProcessDayTint(true); // Catch up time
            SetupStartMenuWindow();
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeInToStartMenu);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        public unsafe void ReturnToFieldWithFadeInAfterEvolutionCheck()
        {
            if (Evolution.GetNextPendingEvolution(out (PartyPokemon, EvolutionData.EvoData) pending))
            {
                (PartyPokemon pkmn, EvolutionData.EvoData evo) = pending;
                _ = new EvolutionGUI(pkmn, evo);
                return;
            }
            ReturnToFieldWithFadeIn();
        }
        public unsafe void ReturnToFieldWithFadeIn()
        {
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeIn);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private static void LoadMapMusic()
        {
            SoundControl.SetOverworldBGM(PlayerObj.Player.Map.MapDetails.Music);
        }

        private unsafe void CB_FadeIn()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Game.Instance.IsOnOverworld = true;
                Game.Instance.SetCallback(CB_LogicTick);
                Game.Instance.SetRCallback(RCB_RenderOverworld);
            }
        }
        private unsafe void CB_FadeInToStartMenu()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Game.Instance.IsOnOverworld = true;
                Game.Instance.SetCallback(CB_StartMenu);
                Game.Instance.SetRCallback(RCB_RenderOverworld);
            }
        }
        private unsafe void CB_FadeOutToWarp()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Obj player = PlayerObj.Player;
                player.Warp(_warpingTo);
                _warpingTo = null;
                if (player.QueuedScriptMovements.Count > 0)
                {
                    player.RunNextScriptMovement();
                    player.IsScriptMoving = true;
                }
                ProcessDayTint(true); // Catch up time
                _fadeTransition = new FadeFromColorTransition(500, 0);
                Game.Instance.SetCallback(CB_FadeIn);
                Game.Instance.SetRCallback(RCB_Fading);
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
                _ = new PartyGUI(Game.Instance.Save.PlayerParty, PartyGUI.Mode.PkmnMenu, ReturnToStartMenuWithFadeIn);
            }
        }
        private void CB_FadeOutToParty_SelectDaycare()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _ = new PartyGUI(Game.Instance.Save.PlayerParty, PartyGUI.Mode.SelectDaycare, ReturnToFieldWithFadeIn);
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
                _ = new BagGUI(Game.Instance.Save.PlayerInventory, Game.Instance.Save.PlayerParty, ReturnToStartMenuWithFadeIn);
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
                _ = new PCBoxesGUI(Game.Instance.Save.PCBoxes, Game.Instance.Save.PlayerParty, ReturnToStartMenuWithFadeIn);
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
            Game.Instance.ProcessScripts();
            Game.Instance.ProcessStringPrinters();
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

        public unsafe void ReturnToFieldAndUseSurf()
        {
            _startMenuWindow?.Close();
            _startMenuWindow = null;
            foreach (Obj o in Obj.LoadedObjs)
            {
                o.IsLocked = true;
            }
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeInToUseSurf);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        private unsafe void CB_FadeInToUseSurf()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Game.Instance.IsOnOverworld = true;
                StartSurfTasks();
                Game.Instance.SetCallback(CB_LogicTick);
                Game.Instance.SetRCallback(RCB_RenderOverworld);
            }
        }

        public void StartSurfTasks()
        {
            PartyPokemon pkmn = Game.Instance.Save.PlayerParty[Game.Instance.Save.Vars[Var.SpecialVar_Result]];
            _tasks.Add(Task_SurfInit, int.MaxValue, data: pkmn);
            // TODO: Clear saved music, start surf music
        }
        private void Task_SurfInit(BackTask task)
        {
            var pkmn = (PartyPokemon)task.Data;
            SoundControl.Debug_PlayCry(pkmn.Species, pkmn.Form);
            task.Data = 0;
            task.Action = Task_Surf_WaitCry;
        }
        private void Task_Surf_WaitCry(BackTask task)
        {
            int num = (int)task.Data;
            if (num < 50)
            {
                task.Data = num + 1;
            }
            else
            {
                PlayerObj player = PlayerObj.Player;
                player.State = PlayerObjState.Surfing;
                player.QueuedScriptMovements.Enqueue(Obj.GetWalkMovement(player.Facing));
                player.RunNextScriptMovement();
                player.IsScriptMoving = true;
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
            _tasks.Remove(task);
        }

        #endregion

        private unsafe void RCB_Fading(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RCB_RenderOverworld(bmpAddress, bmpWidth, bmpHeight);
            _fadeTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
        }
        private unsafe void RCB_RenderOverworld(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.OverwriteRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(0, 0, 0, 255));
            CameraObj.Render(bmpAddress, bmpWidth, bmpHeight);
            if (Overworld.ShouldRenderDayTint())
            {
                DayTint.Render(bmpAddress, bmpWidth, bmpHeight);
            }
            Game.Instance.RenderWindows(bmpAddress, bmpWidth, bmpHeight);
        }
    }
}
