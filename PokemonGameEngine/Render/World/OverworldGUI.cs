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

        // A block is 16x16 pixels (2x2 tiles, and a tile is 8x8 pixels)
        // You can have different sized blocks and tiles if you wish, but this table is demonstrating defaults
        // GB/GBC        - 160 x 144 resolution (10:9) - 10 x  9   blocks
        // GBA           - 240 x 160 resolution ( 3:2) - 15 x 10   blocks
        // NDS           - 256 x 192 resolution ( 4:3) - 16 x 12   blocks
        // 3DS (Lower)   - 320 x 240 resolution ( 4:3) - 20 x 15   blocks
        // 3DS (Upper)   - 400 x 240 resolution ( 5:3) - 25 x 15   blocks
        // Default below - 384 x 216 resolution (16:9) - 24 x 13.5 blocks
        public static readonly Vec2I RenderSize = new(384, 216);

        private readonly FrameBuffer2DColor _frameBuffer;
        private readonly FrameBuffer2DColor _dayTintFrameBuffer;
        private readonly MapRenderer _mapRenderer;
        private readonly TaskList _tasks = new();

        private EventObj _interactiveScriptWaitingFor;
        private string _interactiveScript;
        private ScriptContext _scriptContext;

        private ITransition _transition;

        private OverworldGUI()
        {
            Instance = this;
            Display.SetMinimumWindowSize(RenderSize);

            _frameBuffer = new FrameBuffer2DColor(RenderSize);
            _dayTintFrameBuffer = new FrameBuffer2DColor(RenderSize);
            _mapRenderer = new MapRenderer(RenderSize);
            SetupStartMenuChoices();
            DayTint.SetTintTime();
        }

        public static void Debug_InitOverworldGUI()
        {
            _ = new OverworldGUI(); // Create

            DayTint.CatchUpTime = true;
            StartMapMusic();

            //Instance.ReturnToFieldWithFadeIn();
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
                throw new InvalidOperationException("Tried to warp without the camera.");
            }

            var w = WarpInProgress.Start(warp);
            Song newMusic = w.DestMap.Details.Music;
            if (newMusic != PlayerObj.Instance.Map.Details.Music)
            {
                SoundControl.SetOverworldBGM(newMusic);
            }

            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutToWarp);
        }

        public void StartWildBattle(PBEBattle battle, BattleBackground bg, Song song, IReadOnlyList<Party> trainerParties)
        {
            BattleGUI.CreateWildBattle(battle, bg, ReturnToFieldWithFadeInAfterEvolutionCheck, trainerParties);
            StartBattle(song);
        }
        public void StartTrainerBattle(PBEBattle battle, BattleBackground bg, Song song, IReadOnlyList<Party> trainerParties, TrainerClass c, string defeatText)
        {
            BattleGUI.CreateTrainerBattle(battle, bg, ReturnToFieldWithFadeInAfterEvolutionCheck, trainerParties, c, defeatText);
            StartBattle(song);
        }
        /// <summary>Sets up the battle transition, starts music, sets transition callbacks.</summary>
        private void StartBattle(Song song)
        {
            Game.Instance.IsOnOverworld = false;
            SoundControl.SetBattleBGM(song);

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
            if (Evolution.TryGetNextPendingEvolution(out (PartyPokemon Pkmn, EvolutionData.EvoData Evo) e))
            {
                _ = new EvolutionGUI(e.Pkmn, e.Evo);
                return;
            }
            ReturnToFieldWithFadeIn();
        }

        private static void StartMapMusic()
        {
            SoundControl.SetOverworldBGM(PlayerObj.Instance.Map.Details.Music);
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
            StringPrinter.UpdateAll();
            _tasks.RunTasks();
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
                StartScript(script);
            }
        }

        private void Render()
        {
            GL gl = Display.OpenGL;
            _frameBuffer.Use(gl);
            gl.ClearColor(Colors.Black3);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            _mapRenderer.Render(_frameBuffer);
            DayTint.Render(_frameBuffer, _dayTintFrameBuffer);
            Window.RenderAll();
        }
    }
}
