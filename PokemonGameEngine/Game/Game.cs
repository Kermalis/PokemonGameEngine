using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Overworld;
using Kermalis.PokemonGameEngine.Pkmn;
using System.Threading;

namespace Kermalis.PokemonGameEngine.Game
{
    internal static class Game
    {
        private const int NumTicksPerSecond = 20;

        private static readonly OverworldGUI _overworldGUI;
        private static FadeFromColorTransition _fadeFromTransition;
        private static SpiralTransition _battleTransition;
        private static BattleGUI _battleGUI;

        static Game()
        {
            Save _ = Save.Instance; // Load/initialize Save
            var map = Map.LoadOrGet(0);
            const int x = 2;
            const int y = 12;
            Obj.Camera.X = x;
            Obj.Camera.Y = y;
            Obj.Camera.Map = map;
            map.Objs.Add(Obj.Camera);
            Obj.Player.X = x;
            Obj.Player.Y = y;
            Obj.Player.Map = map;
            map.Objs.Add(Obj.Player);
            _overworldGUI = new OverworldGUI();
            new Thread(LogicThreadMainLoop) { Name = "Logic Thread" }.Start();
        }

        private static void LogicThreadMainLoop()
        {
            while (true)
            {
                LogicTick();
                Thread.Sleep(1000 / NumTicksPerSecond);
            }
        }

        // Temp - start a test wild battle
        public static void TempCreateBattle(EncounterTable.Encounter encounter)
        {
            Save sav = Save.Instance;
            var me = new PBETrainerInfo(sav.PlayerParty, sav.PlayerName);
            var wildPkmn = PartyPokemon.GetTestWildPokemon(encounter);
            var wild = new PBETrainerInfo(new Party { wildPkmn }, "Wild " + PBELocalizedString.GetSpeciesName(wildPkmn.Species).English);
            void OnBattleEnded()
            {
                _battleGUI = null;
                void FadeFromTransitionEnded()
                {
                    _fadeFromTransition = null;
                }
                _fadeFromTransition = new FadeFromColorTransition(20, 0, FadeFromTransitionEnded);
            }
            _battleGUI = new BattleGUI(new PBEBattle(PBEBattleFormat.Single, PBESettings.DefaultSettings, me, wild), OnBattleEnded);
            void OnBattleTransitionEnded()
            {
                _battleTransition = null;
            }
            _battleTransition = new SpiralTransition(OnBattleTransitionEnded);
        }

        private static void LogicTick()
        {
            if (_battleTransition != null || _fadeFromTransition != null)
            {
                return;
            }
            if (_battleGUI != null)
            {
                _battleGUI.LogicTick();
                return;
            }
            _overworldGUI.LogicTick();
        }

        public static unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            if (_battleTransition != null)
            {
                _battleTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                return;
            }
            if (_battleGUI != null)
            {
                _battleGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                return;
            }
            _overworldGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
            if (_fadeFromTransition != null)
            {
                _fadeFromTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                return;
            }
        }
        public static unsafe void RenderFPS(uint* bmpAddress, int bmpWidth, int bmpHeight, int fps)
        {
            Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, 0, 0, fps.ToString(), Font.DefaultFemale);
        }
    }
}
