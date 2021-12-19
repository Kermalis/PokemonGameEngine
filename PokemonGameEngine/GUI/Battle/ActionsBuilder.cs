using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Linq;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed class ActionsBuilder
    {
        private int _index;
        private readonly PBEBattlePokemon[] _pkmn;
        private readonly PBETurnAction[] _actions;
        private readonly PBEBattlePokemon[] _standBy;

        public ActionsBuilder(PBETrainer trainer)
        {
            BattleGUI.Instance.ActionsBuilder = this; // Set here so it's set before the constructor ends
            _pkmn = trainer.ActiveBattlersOrdered.ToArray();
            _actions = new PBETurnAction[_pkmn.Length];
            _standBy = new PBEBattlePokemon[_pkmn.Length];
            ActionsLoop();
        }

        public bool IsStandBy(PBEBattlePokemon p)
        {
            int i = Array.IndexOf(_standBy, p);
            return i != -1 && i < _index;
        }

        public void Pop()
        {
            _index--;
            ActionsLoop();
        }
        public void PushItem(PBEItem item)
        {
            PBEBattlePokemon pkmn = _pkmn[_index];
            var a = new PBETurnAction(pkmn, item);
            pkmn.TurnAction = a;
            _actions[_index] = a;
            _standBy[_index] = null;
            _index++;
            ActionsLoop();
        }
        public void PushMove(PBEMove move, PBETurnTarget targets)
        {
            PBEBattlePokemon pkmn = _pkmn[_index];
            var a = new PBETurnAction(pkmn, move, targets);
            pkmn.TurnAction = a;
            _actions[_index] = a;
            _standBy[_index] = null;
            _index++;
            ActionsLoop();
        }
        public void PushSwitch(PBEBattlePokemon switcher)
        {
            PBEBattlePokemon pkmn = _pkmn[_index];
            var a = new PBETurnAction(pkmn, switcher);
            pkmn.TurnAction = a;
            _actions[_index] = a;
            _standBy[_index] = switcher;
            _index++;
            ActionsLoop();
        }

        private void ActionsLoop()
        {
            BattleGUI bg = BattleGUI.Instance;
            if (_index == _pkmn.Length)
            {
                bg.ActionsBuilder = null;
                bg.SubmitActions(_actions);
            }
            else
            {
                bg.NextAction(_index, _pkmn[_index]);
            }
        }
    }
}
