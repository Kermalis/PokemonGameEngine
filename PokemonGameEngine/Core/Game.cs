using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Script;
using Kermalis.PokemonGameEngine.Trainer;
using Kermalis.PokemonGameEngine.World;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Core
{
    internal delegate void MainCallback();
    internal unsafe delegate void RenderCallback(uint* bmpAddress, int bmpWidth, int bmpHeight);

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

        private static PBEBattleTerrain UpdateBattleSetting(BlocksetBlockBehavior blockBehavior)
        {
            PBEBattleTerrain terrain = Overworld.GetPBEBattleTerrainFromBlock(blockBehavior);
            BattleEngineDataProvider.Instance.UpdateBattleSetting(isCave: terrain == PBEBattleTerrain.Cave,
                isDarkGrass: blockBehavior == BlocksetBlockBehavior.Grass_SpecialEncounter,
                isFishing: false,
                isSurfing: blockBehavior == BlocksetBlockBehavior.Surf,
                isUnderwater: false);
            return terrain;
        }
        private void CreateBattle(PBEBattle battle, Song song, IReadOnlyList<Party> trainerParties, TrainerClass c = default, string defeatText = null)
        {
            OverworldGUI.Instance.StartBattle(battle, song, trainerParties, c: c, defeatText: defeatText);
            Save.GameStats[GameStat.TotalBattles]++;
        }
        public void CreateWildBattle(MapWeather mapWeather, BlocksetBlockBehavior blockBehavior, Party wildParty, PBEBattleFormat format, Song song)
        {
            Save sav = Save;
            var me = new PBETrainerInfo(sav.PlayerParty, sav.OT.TrainerName, true, inventory: sav.PlayerInventory.ToPBEInventory());
            var trainerParties = new Party[] { sav.PlayerParty, wildParty };
            var wild = new PBEWildInfo(wildParty);
            PBEBattleTerrain terrain = UpdateBattleSetting(blockBehavior);
            var battle = new PBEBattle(format, PkmnConstants.PBESettings, me, wild, battleTerrain: terrain, weather: Overworld.GetPBEWeatherFromMap(mapWeather));
            CreateBattle(battle, song, trainerParties);
            Save.GameStats[GameStat.WildBattles]++;
        }
        public void CreateTrainerBattle_1v1(MapWeather mapWeather, BlocksetBlockBehavior blockBehavior, Party[] trainerParties, PBETrainerInfo enemyInfo, PBEBattleFormat format, Song song, TrainerClass c, string defeatText)
        {
            Save sav = Save;
            var me = new PBETrainerInfo(sav.PlayerParty, sav.OT.TrainerName, true, inventory: sav.PlayerInventory.ToPBEInventory());
            PBEBattleTerrain terrain = UpdateBattleSetting(blockBehavior);
            var battle = new PBEBattle(format, PkmnConstants.PBESettings, me, enemyInfo, battleTerrain: terrain, weather: Overworld.GetPBEWeatherFromMap(mapWeather));
            CreateBattle(battle, song, trainerParties, c: c, defeatText: defeatText);
            Save.GameStats[GameStat.TrainerBattles]++;
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
            , string topLeftMessage, Font messageFont, uint[] messageColors
#endif
            )
        {
            RCallback?.Invoke(bmpAddress, bmpWidth, bmpHeight);
#if DEBUG
            if (topLeftMessage is not null)
            {
                messageFont.DrawString(bmpAddress, bmpWidth, bmpHeight, 0, 0, topLeftMessage, messageColors);
            }
#endif
        }

        #endregion
    }
}
