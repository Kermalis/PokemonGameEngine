using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.GUI.Pkmn;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed class SwitchesBuilder
    {
        private readonly byte _switchesRequired;

        private int _index;
        private readonly PBESwitchIn[] _switches;
        private readonly PBEBattlePokemon[] _standBy;
        private readonly PBEFieldPosition[] _positionStandBy;

        public SwitchesBuilder(byte amount)
        {
            BattleGUI.Instance.SwitchesBuilder = this; // Set here so it's set before the constructor ends
            _switchesRequired = amount;
            _switches = new PBESwitchIn[amount];
            _standBy = new PBEBattlePokemon[amount];
            _positionStandBy = new PBEFieldPosition[amount];
        }

        public bool IsStandBy(PBEBattlePokemon p)
        {
            int i = Array.IndexOf(_standBy, p);
            return i != -1 && i < _index;
        }
        public bool IsStandBy(PBEFieldPosition p)
        {
            int i = Array.IndexOf(_positionStandBy, p);
            return i != -1 && i < _index;
        }
        public bool CanPop()
        {
            return _index > 0;
        }
        public int GetNumRemaining()
        {
            return _switchesRequired - _index;
        }

        public void Pop()
        {
            _index--;
            SwitchesLoop();
        }
        public void Push(PBEBattlePokemon pkmn, PBEFieldPosition pos)
        {
            _switches[_index] = new PBESwitchIn(pkmn, pos);
            _standBy[_index] = pkmn;
            _positionStandBy[_index] = pos;
            _index++;
            SwitchesLoop();
        }

        public void SwitchesLoop()
        {
            // Don't handle reaching all switches, we want PartyGUI to handle it
            if (_index != _switchesRequired)
            {
                PartyGUI.Instance.NextSwitch();
            }
        }

        public void Submit()
        {
            BattleGUI bg = BattleGUI.Instance;
            bg.SwitchesBuilder = null;
            bg.SubmitSwitches(_switches);
        }
    }
}
