using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Script;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Core
{
    internal delegate void MainCallback();
    internal unsafe delegate void RenderCallback(uint* bmpAddress, int bmpWidth, int bmpHeight);
    internal delegate void SoundCallback();

    internal sealed class Game
    {
        public static Game Instance { get; private set; }

        public Save Save { get; }
        public StringBuffers StringBuffers { get; }

        public readonly List<ScriptContext> Scripts = new();
        public readonly List<StringPrinter> StringPrinters = new();
        public readonly List<Window> Windows = new();

        /// <summary>For use with Script command "AwaitReturnToField"</summary>
        public bool IsOnOverworld;

        public MainCallback Callback;
        public RenderCallback RCallback;
        public SoundCallback SCallback;

        public Game()
        {
            Instance = this;
            Save = new Save();
            Save.Debug_Create(); // Load/initialize Save
            StringBuffers = new StringBuffers();

            OverworldGUI.Debug_InitOverworldGUI();
        }

        public void SetCallback(MainCallback callback)
        {
#if DEBUG
            if (callback is null)
            {
                System.Console.WriteLine("  Main Callback\t\tnull");
            }
            else
            {
                System.Console.WriteLine("  Main Callback\t\t{0} . {1}", callback.Method.DeclaringType.Name, callback.Method);
            }
#endif
            Callback = callback;
        }
        public void SetRCallback(RenderCallback callback)
        {
#if DEBUG
            if (callback is null)
            {
                System.Console.WriteLine("Render Callback\t\tnull");
            }
            else
            {
                System.Console.WriteLine("Render Callback\t\t{0} . {1}", callback.Method.DeclaringType.Name, callback.Method);
            }
#endif
            RCallback = callback;
        }
        public void SetSCallback(SoundCallback callback)
        {
#if DEBUG
            if (callback is null)
            {
                System.Console.WriteLine(" Sound Callback\t\tnull");
            }
            else
            {
                System.Console.WriteLine(" Sound Callback\t\t{0} . {1}", callback.Method.DeclaringType.Name, callback.Method);
            }
#endif
            SCallback = callback;
        }

        private static PBEBattleTerrain UpdateBattleSetting(Map.Layout.Block block)
        {
            PBEBattleTerrain terrain = Overworld.GetPBEBattleTerrainFromBlock(block.BlocksetBlock);
            BattleEngineDataProvider.Instance.UpdateBattleSetting(isCave: terrain == PBEBattleTerrain.Cave,
                isDarkGrass: block.BlocksetBlock.Behavior == BlocksetBlockBehavior.Grass_SpecialEncounter,
                isFishing: false,
                isSurfing: block.BlocksetBlock.Behavior == BlocksetBlockBehavior.Surf,
                isUnderwater: false);
            return terrain;
        }
        private void CreateBattle(PBEBattle battle, Song song, IReadOnlyList<Party> trainerParties)
        {
            OverworldGUI.Instance.StartBattle(battle, song, trainerParties);
            Save.GameStats[GameStat.TotalBattles]++;
        }
        private void CreateWildBattle(Map map, Map.Layout.Block block, Party wildParty, PBEBattleFormat format, Song song)
        {
            Save sav = Save;
            var me = new PBETrainerInfo(sav.PlayerParty, sav.OT.TrainerName, true, inventory: sav.PlayerInventory.ToPBEInventory());
            var trainerParties = new Party[] { sav.PlayerParty, wildParty };
            var wild = new PBEWildInfo(wildParty);
            PBEBattleTerrain terrain = UpdateBattleSetting(block);
            var battle = new PBEBattle(format, PkmnConstants.PBESettings, me, wild, battleTerrain: terrain, weather: Overworld.GetPBEWeatherFromMap(map));
            CreateBattle(battle, song, trainerParties);
            Save.GameStats[GameStat.WildBattles]++;
        }
        // Temp - start a test wild battle
        public void TempCreateWildBattle(Map map, Map.Layout.Block block, EncounterTable.Encounter encounter)
        {
            CreateWildBattle(map, block, new Party { new PartyPokemon(encounter) }, PBEBattleFormat.Single, Song.WildBattle);
        }
        // For scripted
        public void TempCreateWildBattle(PartyPokemon wildPkmn)
        {
            PlayerObj player = PlayerObj.Player;
            Map.Layout.Block block = player.GetBlock();
            CreateWildBattle(player.Map, block, new Party { wildPkmn }, PBEBattleFormat.Single, Song.LegendaryBattle);
        }

        #region Logic Tick

        public void ProcessScripts()
        {
            foreach (ScriptContext ctx in Scripts.ToArray()) // Copy the list so a script ending/starting does not crash here
            {
                ctx.LogicTick();
            }
        }
        public void ProcessStringPrinters()
        {
            foreach (StringPrinter s in StringPrinters.ToArray())
            {
                s.LogicTick();
            }
        }
        public void LogicTick()
        {
            Callback?.Invoke();
            SCallback?.Invoke();
        }

        #endregion

        #region Render Tick

        public unsafe void RenderWindows(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            foreach (Window w in Windows)
            {
                w.Render(bmpAddress, bmpWidth, bmpHeight);
            }
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight
#if DEBUG
            , string topLeftMessage
#endif
            )
        {
            RCallback?.Invoke(bmpAddress, bmpWidth, bmpHeight);
#if DEBUG
            if (topLeftMessage != null)
            {
                Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, 0, 0, topLeftMessage, Font.DefaultRed_O);
            }
#endif
        }

        #endregion
    }
}
