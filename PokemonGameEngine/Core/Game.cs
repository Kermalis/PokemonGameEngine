using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Script;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class Game
    {
        public static Game Instance { get; private set; }

        public Save Save { get; }
        public StringBuffers StringBuffers { get; }

        public readonly List<ScriptContext> Scripts = new List<ScriptContext>();
        public readonly List<MessageBox> MessageBoxes = new List<MessageBox>();

        /// <summary>For use with Script command "AwaitBattle"</summary>
        public bool IsOnOverworld;

        public delegate void MainCallback();
        public MainCallback Callback;
        public uint CBState;
        public unsafe delegate void RenderCallback(uint* bmpAddress, int bmpWidth, int bmpHeight);
        public RenderCallback RCallback;

        public Game()
        {
            Instance = this;
            Save = new Save(); // Load/initialize Save
            StringBuffers = new StringBuffers();

            OverworldGUI.Debug_InitOverworldGUI();
        }

        public void SetCallback(MainCallback callback)
        {
#if DEBUG
            System.Console.WriteLine("  Main Callback\t\t{0} . {1}", callback.Method.DeclaringType.Name, callback.Method);
#endif
            Callback = callback;
            CBState = 0;
        }
        public void SetRCallback(RenderCallback callback)
        {
#if DEBUG
            System.Console.WriteLine("Render Callback\t\t{0} . {1}", callback.Method.DeclaringType.Name, callback.Method);
#endif
            RCallback = callback;
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
            OverworldGUI.Instance.StartBattle(battle, trainerParties);
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

        #region Logic Tick

        public void ProcessScripts()
        {
            foreach (ScriptContext ctx in Scripts.ToArray()) // Copy the list so a script ending/starting does not crash here
            {
                ctx.LogicTick();
            }
        }
        public void ProcessMessageBoxes()
        {
            foreach (MessageBox mb in MessageBoxes.ToArray())
            {
                mb.LogicTick();
            }
        }
        public void LogicTick()
        {
            Callback?.Invoke();
        }

        #endregion

        #region Render Tick

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight
#if DEBUG
            , string topLeftMessage
#endif
            )
        {
            RCallback?.Invoke(bmpAddress, bmpWidth, bmpHeight);
            // Render messagebox
            foreach (MessageBox mb in MessageBoxes.ToArray())
            {
                mb.Render(bmpAddress, bmpWidth, bmpHeight);
            }
#if DEBUG
            if (topLeftMessage != null)
            {
                Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, 0, 0, topLeftMessage, Font.DefaultFemale);
            }
#endif
        }

        #endregion
    }
}
