using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Game;
using Kermalis.PokemonGameEngine.Overworld;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Scripts;
using System.IO;

namespace Kermalis.PokemonGameEngine.Script
{
    internal sealed partial class ScriptContext
    {
        public void RunNextCommand()
        {
            ScriptCommand cmd = _reader.ReadEnum<ScriptCommand>();
            switch (cmd)
            {
                case ScriptCommand.End: EndCommand(); break;
                case ScriptCommand.GoTo: GoToCommand(); break;
                case ScriptCommand.Call: CallCommand(); break;
                case ScriptCommand.Return: ReturnCommand(); break;
                case ScriptCommand.HealParty: HealPartyCommand(); break;
                case ScriptCommand.GivePokemon: GivePokemonCommand(); break;
                case ScriptCommand.GivePokemonForm: GivePokemonFormCommand(); break;
                case ScriptCommand.GivePokemonFormItem: GivePokemonFormItemCommand(); break;
                case ScriptCommand.MoveObj: MoveObjCommand(); break;
                case ScriptCommand.AwaitObjMovement: AwaitObjMovementCommand(); break;
                default: throw new InvalidDataException();
            }
        }

        private void EndCommand()
        {
            Dispose();
        }

        private void GoToCommand()
        {
            uint offset = _reader.ReadUInt32();
            _reader.BaseStream.Position = offset;
        }

        private void CallCommand()
        {
            uint offset = _reader.ReadUInt32();
            _callStack.Push(_reader.BaseStream.Position);
            _reader.BaseStream.Position = offset;
        }

        private void ReturnCommand()
        {
            _reader.BaseStream.Position = _callStack.Pop();
        }

        private void HealPartyCommand()
        {
            Save.Instance.PlayerParty.HealFully();
        }

        private void GivePokemonCommand()
        {
            PBESpecies species = _reader.ReadEnum<PBESpecies>();
            byte level = _reader.ReadByte();
            var pkmn = PartyPokemon.GetTestPokemon(species, 0, level);
            Save.Instance.GivePokemon(pkmn);
        }

        private void GivePokemonFormCommand()
        {
            PBESpecies species = _reader.ReadEnum<PBESpecies>();
            PBEForm form = _reader.ReadEnum<PBEForm>();
            byte level = _reader.ReadByte();
            var pkmn = PartyPokemon.GetTestPokemon(species, form, level);
            Save.Instance.GivePokemon(pkmn);
        }

        private void GivePokemonFormItemCommand()
        {
            PBESpecies species = _reader.ReadEnum<PBESpecies>();
            PBEForm form = _reader.ReadEnum<PBEForm>();
            byte level = _reader.ReadByte();
            PBEItem item = _reader.ReadEnum<PBEItem>();
            var pkmn = PartyPokemon.GetTestPokemon(species, form, level);
            pkmn.Item = item;
            Save.Instance.GivePokemon(pkmn);
        }

        private void MoveObjCommand()
        {
            ushort id = _reader.ReadUInt16();
            uint offset = _reader.ReadUInt32();
            _callStack.Push(_reader.BaseStream.Position);
            _reader.BaseStream.Position = offset;
            var obj = Obj.GetObj(id);
            while (true)
            {
                ScriptMovement m = _reader.ReadEnum<ScriptMovement>();
                if (m == ScriptMovement.End)
                {
                    break;
                }
                obj.QueuedScriptMovements.Enqueue(m);
            }
            _reader.BaseStream.Position = _callStack.Pop();
            obj.RunNextScriptMovement();
        }

        private void AwaitObjMovementCommand()
        {
            ushort id = _reader.ReadUInt16();
            var obj = Obj.GetObj(id);
            _waitMovementObj = obj;
        }
    }
}
