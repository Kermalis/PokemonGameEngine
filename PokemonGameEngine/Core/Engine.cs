#if DEBUG
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using System;
using System.IO;
using System.Runtime.CompilerServices;
#endif
using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Trainer;
using Kermalis.PokemonGameEngine.World;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Core
{
    internal delegate void MainCallback();
    internal delegate void RenderCallback(GL gl);

    internal sealed class Engine
    {
        public static Engine Instance { get; private set; }

        public Save Save { get; }
        public StringBuffers StringBuffers { get; }

        /// <summary>For use with Script command "AwaitReturnToField"</summary>
        public bool IsOnOverworld; // TODO: Convert into a sort of general purpose "WaitState"/"WaitSignal" command

        public MainCallback Callback;
        public RenderCallback RCallback;

        public Engine()
        {
            Instance = this;
            Save = new Save();
            Save.Debug_Create(); // Load/initialize Save
            StringBuffers = new StringBuffers();

            OverworldGUI.Debug_InitOverworldGUI();
        }

#if DEBUG
        public void SetCallback(MainCallback callback, [CallerMemberName] string caller = null, [CallerFilePath] string callerFile = null)
        {
            callerFile = Path.GetFileName(callerFile);
            if (callback is null)
            {
                Console.WriteLine("  Main Callback\t\tnull\t\tFrom {0}()\t[{1}]", caller, callerFile);
            }
            else
            {
                Console.WriteLine("  Main Callback\t\t{0}.{1}()\t\tFrom {2}()\t[{3}]", callback.Method.DeclaringType.Name, callback.Method.Name, caller, callerFile);
            }
            Callback = callback;
        }
        public void SetRCallback(RenderCallback callback, [CallerMemberName] string caller = null, [CallerFilePath] string callerFile = null)
        {
            callerFile = Path.GetFileName(callerFile);
            if (callback is null)
            {
                Console.WriteLine("Render Callback\t\tnull\t\tFrom {0}()\t[{1}]", caller, callerFile);
            }
            else
            {
                Console.WriteLine("Render Callback\t\t{0}.{1}()\t\tFrom {2}()\t[{3}]", callback.Method.DeclaringType.Name, callback.Method.Name, caller, callerFile);
            }
            RCallback = callback;
        }
#else
        public void SetCallback(MainCallback callback)
        {
            Callback = callback;
        }
        public void SetRCallback(RenderCallback callback)
        {
            RCallback = callback;
        }
#endif

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

        public void LogicTick()
        {
            Callback?.Invoke();
        }
#if DEBUG
        public void RenderTick(GL gl, string topLeftMessage, Font messageFont, ColorF[] messageColors)
        {
            RCallback?.Invoke(gl);
            if (topLeftMessage is not null)
            {
                GUIString.CreateAndRenderOneTimeString(gl, topLeftMessage, messageFont, messageColors, default);
            }
        }
#else
        public void RenderTick(GL gl)
        {
            RCallback?.Invoke(gl);
        }
#endif
    }
}
