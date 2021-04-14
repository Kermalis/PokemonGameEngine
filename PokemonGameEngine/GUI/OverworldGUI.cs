using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Script;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;
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
            var map = Map.LoadOrGet(0);
            const int x = 2;
            const int y = 29;
            PlayerObj.Player.Pos.X = x;
            PlayerObj.Player.Pos.Y = y;
            PlayerObj.Player.Map = map;
            map.Objs.Add(PlayerObj.Player);
            CameraObj.Camera.Pos = PlayerObj.Player.Pos;
            CameraObj.Camera.Map = map;
            map.Objs.Add(CameraObj.Camera);
            map.LoadObjEvents();
            new OverworldGUI(); // Create

            ProcessDayTint(true);
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
        private void SetupStartMenuChoices()
        {
            _startMenuChoices = new TextGUIChoices(0.15f, 0.05f, 0.1f, backCommand: CloseStartMenu, font: Font.Default, fontColors: Font.DefaultDark, selectedColors: Font.DefaultSelected);
            _startMenuChoices.Add(new TextGUIChoice("Pokémon", StartMenu_DebugBagSelected));
            _startMenuChoices.Add(new TextGUIChoice("Bag", StartMenu_DebugBagSelected));
            _startMenuChoices.Add(new TextGUIChoice("Close", CloseStartMenu));
        }

        private void SetupStartMenuWindow()
        {
            _startMenuWindow = new Window(0.72f, 0.05f, 0.25f, 0.9f);
            RenderStartMenuChoicesOntoWindow();
        }
        private unsafe void RenderStartMenuChoicesOntoWindow()
        {
            _startMenuWindow.ClearSprite();
            Sprite s = _startMenuWindow.Sprite;
            fixed (uint* bmpAddress = s.Bitmap)
            {
                _startMenuChoices.Render(bmpAddress, s.Width, s.Height);
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

        public unsafe void StartBattle(PBEBattle battle, IReadOnlyList<Party> trainerParties)
        {
            Game.Instance.IsOnOverworld = false;
            new BattleGUI(battle, OnBattleEnded, trainerParties);
            _fadeTransition = new SpiralTransition();
            Game.Instance.SetCallback(CB_FadeOutToBattle);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        public unsafe void TempWarp(IWarp warp)
        {
            _warpingTo = warp;
            _fadeTransition = new FadeToColorTransition(20, 0);
            Game.Instance.SetCallback(CB_FadeOutToWarp);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void OnBagMenuClosed()
        {
            ProcessDayTint(true); // Catch up time
            SetupStartMenuWindow();
            _fadeTransition = new FadeFromColorTransition(20, 0);
            Game.Instance.SetCallback(CB_FadeInToStartMenu);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        private unsafe void OnBattleEnded()
        {
            _fadeTransition = new FadeFromColorTransition(20, 0);
            Game.Instance.SetCallback(CB_FadeIn);
            Game.Instance.SetRCallback(RCB_Fading);
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
        private void CB_FadeOutToBag()
        {
            Tileset.AnimationTick();
            ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _startMenuWindow.Close();
                _startMenuWindow = null;
                new BagGUI(Game.Instance.Save.PlayerInventory, Game.Instance.Save.PlayerParty, OnBagMenuClosed);
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
