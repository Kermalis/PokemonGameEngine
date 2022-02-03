using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render.Battle;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Pkmn;
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
    internal sealed partial class OverworldGUI
    {
        public static OverworldGUI Instance { get; private set; } = null!; // Set in constructor

        // By default, a block is 16x16 pixels -- 2x2 tiles, 8x8 pixels per tile
        // |  CONSOLE  |    RESOLUTION    | NUM BLOCKS |
        // |-----------|------------------|------------|
        // | GB && GBC | 160 x 144 (10:9) | 10 x  9    |
        // |    GBA    | 240 x 160 ( 3:2) | 15 x 10    |
        // |    NDS    | 256 x 192 ( 4:3) | 16 x 12    |
        // | 3DS Lower | 320 x 240 ( 4:3) | 20 x 15    |
        // | 3DS Upper | 400 x 240 ( 5:3) | 25 x 15    |
        // |-----------|------------------|------------|
        // |   Below   | 320 x 180 (16:9) | 20 x 11.25 |
        public static readonly Vec2I RenderSize = new(320, 180);

        private readonly FrameBuffer _frameBuffer;
        private readonly FrameBuffer _dayTintFrameBuffer;
        private readonly MapRenderer _mapRenderer;
        private readonly ConnectedList<BackTask> _tasks = new(BackTask.Sorter);

        private EventObj _interactiveScriptWaitingFor;
        private string _interactiveScript;
        private ScriptContext _scriptContext;

        private ITransition _transition;

        private OverworldGUI()
        {
            Instance = this;
            Display.SetMinimumWindowSize(RenderSize);

            _frameBuffer = new FrameBuffer().AddColorTexture(RenderSize);
            _dayTintFrameBuffer = new FrameBuffer().AddColorTexture(RenderSize);
            _mapRenderer = new MapRenderer(RenderSize);
            SetupStartMenuChoices();
            DayTint.SetTintTime();
        }

        public static void Debug_InitOverworldGUI()
        {
            _ = new OverworldGUI(); // Create

            DayTint.CatchUpTime = true;
            FadeToMapMusic();

            Instance.ReturnToFieldWithFadeIn();
            //EncounterMaker.Debug_CreateTestWildBattle();
            //TrainerCore.Debug_CreateTestTrainerBattle();
        }

        public void SetInteractiveScript(EventObj talkedTo, string script)
        {
            Game.Instance.Save.Vars[Var.LastTalked] = (short)talkedTo.Id; // Special var for the last person we talked to
            talkedTo.TalkedTo = true;
            PlayerObj.Instance.IsWaitingForObjToStartScript = true;
            _interactiveScriptWaitingFor = talkedTo;
            _interactiveScript = script;
        }
        public void StartScript(string label)
        {
            _scriptContext = ScriptLoader.LoadScript(label, RenderSize);
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
        public void StartPlayerWarp(in Warp warp)
        {
            if (CameraObj.Instance.CamAttachedTo?.Id != Overworld.PlayerId)
            {
                throw new InvalidOperationException("Player tried to warp without the camera.");
            }

            WarpInProgress.Current = new WarpInProgress(warp);
            MusicPlayer.Main.QueueMusicIfDifferentThenFadeOutCurrentMusic(WarpInProgress.Current.DestMap.Details.Music);

            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutToWarp);
        }

        public void StartWildBattle(PBEBattle battle, BattleBackground bg, Song music, IReadOnlyList<Party> trainerParties)
        {
            BattleGUI.CreateWildBattle(battle, bg, ReturnToFieldWithFadeInAfterEvolutionCheck, trainerParties);
            StartBattle(music);
        }
        public void StartTrainerBattle(PBEBattle battle, BattleBackground bg, Song music, IReadOnlyList<Party> trainerParties, TrainerClass c, string defeatText)
        {
            BattleGUI.CreateTrainerBattle(battle, bg, ReturnToFieldWithFadeInAfterEvolutionCheck, trainerParties, c, defeatText);
            StartBattle(music);
        }
        /// <summary>Sets up the battle transition, starts music, sets transition callbacks.</summary>
        private void StartBattle(Song music)
        {
            Game.Instance.IsOnOverworld = false;
            MusicPlayer.Main.BeginNewMusicAndBackupCurrentMusic(music);

            _transition = new BattleTransition_Liquid(RenderSize);
            Game.Instance.SetCallback(CB_FadeOutToBattle);
        }

        /// <summary>Starts a fade from black fade, sets the size, and sets the callback to fade in</summary>
        public void ReturnToFieldWithFadeIn()
        {
            Display.SetMinimumWindowSize(RenderSize);

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeIn);
        }
        public void ReturnToFieldWithFadeInAfterEvolutionCheck()
        {
            if (Evolution.TryGetNextPendingEvolution(out (PartyPokemon Pkmn, EvolutionData.EvoData Evo, bool CanCancel) e))
            {
                _ = new EvolutionGUI(e.Pkmn, e.Evo, e.CanCancel);
                return;
            }
            ReturnToFieldWithFadeIn();
        }

        public static void FadeToMapMusic()
        {
            MusicPlayer.Main.FadeToNewMusic(CameraObj.Instance.Map.Details.Music);
        }

        private void CB_FadeIn()
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
            Game.Instance.SetCallback(CB_ProcessScriptsTasksAndObjs);
        }
        private void CB_FadeOutToWarp()
        {
            Render();
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            DayTint.CatchUpTime = true;
            PlayerObj player = PlayerObj.Instance;
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
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            _ = new EggHatchGUI();
        }
        private void CB_FadeOutToBattle()
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
            BattleGUI.Instance.InitFadeIn();
        }
        private void CB_ProcessScriptsTasksAndObjs()
        {
            if (_scriptContext is not null)
            {
                _scriptContext.Update();
                if (_scriptContext.IsDead)
                {
                    _scriptContext = null;
                }
            }
            for (StringPrinter s = StringPrinter.AllStringPrinters.First; s is not null; s = s.Next)
            {
                s.Update();
            }
            for (BackTask t = _tasks.First; t is not null; t = t.Next)
            {
                t.Action(t);
            }
            ProcessObjs();

#if DEBUG_OVERWORLD
            _mapRenderer.Debug_CheckToggleInput();
#endif
            Render();
            _frameBuffer.BlitToScreen();
#if DEBUG_OVERWORLD
            _mapRenderer.Debug_RenderToScreen();
#endif
        }

        private void ProcessObjs()
        {
            for (Obj o = Obj.LoadedObjs.First; o is not null; o = o.Next)
            {
                if (o.ShouldUpdateMovement)
                {
                    o.UpdateMovement();
                }
            }
            for (Obj o = Obj.LoadedObjs.First; o is not null; o = o.Next)
            {
                if (!o.IsDead)
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
                StartScript(script);
            }
        }

        private void Render()
        {
            GL gl = Display.OpenGL;
            _frameBuffer.UseAndViewport(gl);
            gl.ClearColor(Colors.Black3);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            _mapRenderer.Render(_frameBuffer);
            DayTint.Render(_frameBuffer, _dayTintFrameBuffer);
            Window.RenderAll();
        }
    }
}
