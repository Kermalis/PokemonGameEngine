using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Pkmn;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
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
            var gui = new OverworldGUI(); // Create

            ProcessDayTint(true);
            gui.LoadMapMusic();
            Instance._fadeTransition = new FadeFromColorTransition(20, 0);
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
            _fadeTransition = new FadeToColorTransition(20, 0);
            Game.Instance.SetCallback(CB_FadeOutToBag);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        private unsafe void StartMenu_DebugPCSelected()
        {
            _fadeTransition = new FadeToColorTransition(20, 0);
            Game.Instance.SetCallback(CB_FadeOutToPC);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        private void SetupStartMenuChoices()
        {
            _startMenuChoices = new TextGUIChoices(0, 0, backCommand: CloseStartMenu, font: Font.Default, fontColors: Font.DefaultDark, selectedColors: Font.DefaultSelected);
            _startMenuChoices.Add(new TextGUIChoice("Pokémon", () => OpenPartyMenu(PartyGUI.Mode.PkmnMenu)));
            _startMenuChoices.Add(new TextGUIChoice("Bag", StartMenu_DebugBagSelected));
            _startMenuChoices.Add(new TextGUIChoice("PC", StartMenu_DebugPCSelected));
            _startMenuChoices.Add(new TextGUIChoice("Close", CloseStartMenu));
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
        private void CloseStartMenu()
        {
            _startMenuWindow.Close();
            _startMenuWindow = null;
            Game.Instance.SetCallback(CB_LogicTick);
        }

        public unsafe void OpenPartyMenu(PartyGUI.Mode mode)
        {
            _fadeTransition = new FadeToColorTransition(20, 0);
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
            _fadeTransition = new FadeToColorTransition(20, 0);
            Game.Instance.IsOnOverworld = false;
            Game.Instance.SetCallback(CB_FadeOutToEggHatchScreen);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        public unsafe void StartBattle(PBEBattle battle, Song song, IReadOnlyList<Party> trainerParties)
        {
            Game.Instance.IsOnOverworld = false;
            new BattleGUI(battle, ReturnToFieldWithFadeIn, trainerParties);
            SoundControl.SetBattleBGM(song);
            _fadeTransition = new SpiralTransition();
            Game.Instance.SetCallback(CB_FadeOutToBattle);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        public unsafe void TempWarp(IWarp warp)
        {
            _warpingTo = warp;
            SoundControl.SetOverworldBGM(Map.LoadOrGet(warp.DestMapId).MapDetails.Music);
            _fadeTransition = new FadeToColorTransition(20, 0);
            Game.Instance.SetCallback(CB_FadeOutToWarp);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void ReturnToStartMenuWithFadeIn()
        {
            ProcessDayTint(true); // Catch up time
            SetupStartMenuWindow();
            _fadeTransition = new FadeFromColorTransition(20, 0);
            Game.Instance.SetCallback(CB_FadeInToStartMenu);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        public unsafe void ReturnToFieldWithFadeIn()
        {
            _fadeTransition = new FadeFromColorTransition(20, 0);
            Game.Instance.SetCallback(CB_FadeIn);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private void LoadMapMusic()
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
                }
                ProcessDayTint(true); // Catch up time
                _fadeTransition = new FadeFromColorTransition(20, 0);
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
                new EggHatchGUI();
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
                new PartyGUI(Game.Instance.Save.PlayerParty, PartyGUI.Mode.PkmnMenu, ReturnToStartMenuWithFadeIn);
            }
        }
        private void CB_FadeOutToParty_SelectDaycare()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                new PartyGUI(Game.Instance.Save.PlayerParty, PartyGUI.Mode.SelectDaycare, ReturnToFieldWithFadeIn);
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
                new BagGUI(Game.Instance.Save.PlayerInventory, Game.Instance.Save.PlayerParty, ReturnToStartMenuWithFadeIn);
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
                new PCBoxesGUI(Game.Instance.Save.PCBoxes, Game.Instance.Save.PlayerParty, ReturnToStartMenuWithFadeIn);
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
