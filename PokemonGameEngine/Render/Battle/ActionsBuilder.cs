using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Linq;

namespace Kermalis.PokemonGameEngine.Render.Battle
{
    internal sealed class ActionsBuilder
    {
        private int _index;
        private readonly PBEBattlePokemon[] _pkmn;
        private readonly PBETurnAction[] _actions;
        private readonly PBEBattlePokemon[] _standBy;

        public ActionsBuilder(PBETrainer trainer)
        {
            _pkmn = trainer.ActiveBattlersOrdered.ToArray();
            _actions = new PBETurnAction[_pkmn.Length];
            _standBy = new PBEBattlePokemon[_pkmn.Length];
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

        public void ActionsLoop()
        {
            if (_index == _pkmn.Length)
            {
                BattleGUI.Instance.SubmitActions(_actions);
            }
            else
            {
                BattleGUI.Instance.NextAction(_index, _pkmn[_index]);
            }
        }
    }
}
