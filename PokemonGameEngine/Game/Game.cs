using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Overworld;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Wild;
using Kermalis.PokemonGameEngine.Util;
using System.Threading;

namespace Kermalis.PokemonGameEngine.Game
{
    internal static class Game
    {
        private const int NumTicksPerSecond = 20;

        private static readonly Font _font;
        private static readonly uint[] _fontColors = new uint[] { 0x00000000, 0xFFFFFFFF, 0xFF848484 };

        private static readonly OverworldGUI _overworldGUI = new OverworldGUI();
        private static SpiralTransition _transition;
        private static BattleGUI _battleGUI;

        static Game()
        {
            _font = new Font("TestFont.kermfont");
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
        public static void TempCreateBattle()
        {
            var encounter = WildEncounter.GetTestEncounter();
            var me = new PBETrainerInfo(new Party { PartyPokemon.GetTestPokemon(PBESpecies.Skitty, 0) }, "Dawn");
            var wild = new PBETrainerInfo(new Party { PartyPokemon.GetTestWildPokemon(encounter) }, "WILD");
            _battleGUI = new BattleGUI(new PBEBattle(PBEBattleFormat.Single, PBESettings.DefaultSettings, me, wild));
            void OnTransitionEnded()
            {
                _transition = null;
            }
            _transition = new SpiralTransition(OnTransitionEnded);
        }

        private static void LogicTick()
        {
            if (_transition != null)
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
            Font font = _font;
            uint[] fontColors = _fontColors;
            if (_transition != null)
            {
                _transition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                return;
            }
            if (_battleGUI != null)
            {
                _battleGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight, font, fontColors);
                return;
            }
            _overworldGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
        }
        public static unsafe void RenderFPS(uint* bmpAddress, int bmpWidth, int bmpHeight, int fps)
        {
            RenderUtil.DrawString(bmpAddress, bmpWidth, bmpHeight, 0, 0, fps.ToString(), _font, _fontColors);
        }
    }
}
