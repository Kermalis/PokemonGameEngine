using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Script;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class Game
    {
        private enum GameState : byte
        {
            Init, // Loading
            Overworld, // Overworld
            OverworldToBag, // Fading to black
            Bag, // Bag
            BagFromOverworld, // Fading from black
            BagToOverworld, // Fading to black
            OverworldFromBag, // Fading from black
            OverworldToBattle, // Fading to black
            Battle, // Battling
            BattleFromOverworld, // Fading from black
            BattleToOverworld, // Fading to black
            OverworldFromBattle, // Fading from black
            OverworldWarpOut, // Warp fade
            OverworldWarpIn, // Warp return
        }

        public static Game Instance { get; private set; }

        public Save Save { get; }
        public StringBuffers StringBuffers { get; }

        public readonly List<ScriptContext> Scripts = new List<ScriptContext>();
        public readonly List<MessageBox> MessageBoxes = new List<MessageBox>();

        private GameState _state = GameState.Init;
        public OverworldGUI OverworldGUI { get; }
        private FadeFromColorTransition _fadeFromTransition;
        private FadeToColorTransition _fadeToTransition;
        private SpiralTransition _battleTransition;
        public BattleGUI BattleGUI { get; private set; }
        private BagGUI _bagGUI;

        public Game()
        {
            Instance = this;
            Save = new Save(); // Load/initialize Save
            StringBuffers = new StringBuffers();
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
            OverworldGUI = new OverworldGUI();
            _state = GameState.Overworld;
        }

        public void TempWarp(IWarp warp)
        {
            void FadeToTransitionEnded()
            {
                Obj player = PlayerObj.Player;
                player.Warp(warp);
                void FadeFromTransitionEnded()
                {
                    _state = GameState.Overworld;
                    _fadeFromTransition = null;
                }
                _fadeFromTransition = new FadeFromColorTransition(20, 0, FadeFromTransitionEnded);
                if (player.QueuedScriptMovements.Count > 0)
                {
                    player.RunNextScriptMovement();
                }
                _state = GameState.OverworldWarpIn;
                _fadeToTransition = null;
            }
            _fadeToTransition = new FadeToColorTransition(20, 0, FadeToTransitionEnded);
            _state = GameState.OverworldWarpOut;
        }

        private PBEBattleTerrain UpdateBattleSetting(Map.Layout.Block block)
        {
            PBEBattleTerrain terrain = Overworld.GetPBEBattleTerrainFromBlock(block.BlocksetBlock);
            BattleEngineDataProvider.Instance.UpdateBattleSetting(isCave: terrain == PBEBattleTerrain.Cave,
                isDarkGrass: block.BlocksetBlock.Behavior == BlocksetBlockBehavior.Grass_SpecialEncounter,
                isFishing: false,
                isSurfing: block.BlocksetBlock.Behavior == BlocksetBlockBehavior.Surf,
                isUnderwater: false);
            return terrain;
        }
        private void CreateBattle(PBEBattle battle, IReadOnlyList<Party> trainerParties)
        {
            void FadeToBattleTransitionEnded()
            {
                void OnBattleEnded()
                {
                    void FadeToOverworldTransitionEnded()
                    {
                        void FadeFromBagTransitionEnded()
                        {
                            _state = GameState.Overworld;
                            _fadeFromTransition = null;
                        }
                        _fadeFromTransition = new FadeFromColorTransition(20, 0, FadeFromBagTransitionEnded);
                        _state = GameState.OverworldFromBattle;
                        BattleGUI = null;
                        _fadeToTransition = null;
                    }
                    _fadeToTransition = new FadeToColorTransition(20, 0, FadeToOverworldTransitionEnded);
                    _state = GameState.BattleToOverworld;
                }
                void FadeFromOverworldTransitionEnded()
                {
                    _state = GameState.Battle;
                    _fadeFromTransition = null;
                }
                BattleGUI = new BattleGUI(battle, OnBattleEnded, trainerParties);
                _fadeFromTransition = new FadeFromColorTransition(20, 0, FadeFromOverworldTransitionEnded);
                _state = GameState.BattleFromOverworld;
                _battleTransition = null;
            }
            _battleTransition = new SpiralTransition(FadeToBattleTransitionEnded);
            _state = GameState.OverworldToBattle;
        }
        private void CreateWildBattle(Map map, Map.Layout.Block block, Party wildParty, PBEBattleFormat format)
        {
            Save sav = Save;
            var me = new PBETrainerInfo(sav.PlayerParty, sav.PlayerName, inventory: sav.PlayerInventory.ToPBEInventory());
            var trainerParties = new Party[] { sav.PlayerParty, wildParty };
            var wild = new PBEWildInfo(wildParty);
            PBEBattleTerrain terrain = UpdateBattleSetting(block);
            var battle = new PBEBattle(format, PkmnConstants.PBESettings, me, wild, battleTerrain: terrain, weather: Overworld.GetPBEWeatherFromMap(map));
            CreateBattle(battle, trainerParties);
        }
        // Temp - start a test wild battle
        public void TempCreateWildBattle(Map map, Map.Layout.Block block, EncounterTable.Encounter encounter)
        {
            CreateWildBattle(map, block, new Party { new PartyPokemon(encounter) }, PBEBattleFormat.Single);
        }
        // For scripted
        public void TempCreateWildBattle(PartyPokemon wildPkmn)
        {
            PlayerObj player = PlayerObj.Player;
            Map.Layout.Block block = player.GetBlock(out Map map);
            CreateWildBattle(map, block, new Party { wildPkmn }, PBEBattleFormat.Single);
        }

        public void OpenStartMenu()
        {
            void FadeToBagTransitionEnded()
            {
                void OnBagMenuGUIClosed()
                {
                    void FadeToOverworldTransitionEnded()
                    {
                        void FadeFromBagTransitionEnded()
                        {
                            _state = GameState.Overworld;
                            _fadeFromTransition = null;
                        }
                        _fadeFromTransition = new FadeFromColorTransition(20, 0, FadeFromBagTransitionEnded);
                        _state = GameState.OverworldFromBag;
                        _bagGUI = null;
                        _fadeToTransition = null;
                    }
                    _fadeToTransition = new FadeToColorTransition(20, 0, FadeToOverworldTransitionEnded);
                    _state = GameState.BagToOverworld;
                }
                void FadeFromOverworldTransitionEnded()
                {
                    _state = GameState.Bag;
                    _fadeFromTransition = null;
                }
                _bagGUI = new BagGUI(Save.PlayerInventory, Save.PlayerParty, OnBagMenuGUIClosed);
                _fadeFromTransition = new FadeFromColorTransition(20, 0, FadeFromOverworldTransitionEnded);
                _state = GameState.BagFromOverworld;
                _fadeToTransition = null;
            }
            _fadeToTransition = new FadeToColorTransition(20, 0, FadeToBagTransitionEnded);
            _state = GameState.OverworldToBag;
        }

        #region Logic Tick

        private void ProcessScripts()
        {
            foreach (ScriptContext ctx in Scripts.ToArray()) // Copy the list so a script ending/starting does not crash here
            {
                ctx.LogicTick();
            }
        }
        private void ProcessMessageBoxes()
        {
            foreach (MessageBox mb in MessageBoxes.ToArray())
            {
                mb.LogicTick();
            }
        }
        private void ProcessDayTint(DateTime time, bool skipTransition)
        {
            if (Overworld.ShouldRenderDayTint())
            {
                DayTint.LogicTick(time, skipTransition);
            }
        }
        public void LogicTick()
        {
            DateTime time = DateTime.Now;
            switch (_state)
            {
                case GameState.Overworld:
                {
                    ProcessScripts();
                    ProcessMessageBoxes();
                    Tileset.AnimationTick();
                    ProcessDayTint(time, false);
                    OverworldGUI.LogicTick();
                    return;
                }
                case GameState.OverworldToBag:
                case GameState.OverworldToBattle:
                case GameState.OverworldFromBattle:
                case GameState.OverworldWarpOut:
                {
                    Tileset.AnimationTick();
                    ProcessDayTint(time, false); // Don't want it to suddenly become dark when fading out the overworld
                    return;
                }
                case GameState.OverworldFromBag:
                case GameState.OverworldWarpIn:
                {
                    Tileset.AnimationTick();
                    ProcessDayTint(time, true); // Want the time to automatically be correct when we are on the overworld again (could've been in a cave for hours, or paused in the bag, etc)
                    return;
                }
                case GameState.Bag:
                {
                    _bagGUI.LogicTick();
                    return;
                }
                case GameState.Battle:
                {
                    ProcessDayTint(time, false);
                    BattleGUI.LogicTick();
                    return;
                }
            }
        }

        #endregion

        #region Render Tick

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight, string topLeftMessage)
        {
            switch (_state)
            {
                case GameState.Overworld:
                {
                    OverworldGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    break;
                }
                case GameState.OverworldToBag:
                case GameState.OverworldWarpOut:
                {
                    OverworldGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    _fadeToTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    break;
                }
                case GameState.OverworldFromBattle:
                case GameState.OverworldFromBag:
                case GameState.OverworldWarpIn:
                {
                    OverworldGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    _fadeFromTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    break;
                }
                case GameState.BagFromOverworld:
                {
                    _bagGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    _fadeFromTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    break;
                }
                case GameState.Bag:
                {
                    _bagGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    break;
                }
                case GameState.BagToOverworld:
                {
                    _bagGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    _fadeToTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    break;
                }
                case GameState.OverworldToBattle:
                {
                    OverworldGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    _battleTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    break;
                }
                case GameState.BattleFromOverworld:
                {
                    BattleGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    _fadeFromTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    break;
                }
                case GameState.Battle:
                {
                    BattleGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    break;
                }
                case GameState.BattleToOverworld:
                {
                    BattleGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    _fadeToTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    break;
                }
            }
            // Render messagebox
            foreach (MessageBox mb in MessageBoxes.ToArray())
            {
                mb.Render(bmpAddress, bmpWidth, bmpHeight);
            }
            if (topLeftMessage != null)
            {
                Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, 0, 0, topLeftMessage, Font.DefaultFemale);
            }
        }

        #endregion
    }
}
