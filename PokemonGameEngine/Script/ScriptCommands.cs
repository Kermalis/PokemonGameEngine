using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Scripts;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;
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
                case ScriptCommand.DetachCamera: DetachCameraCommand(); break;
                case ScriptCommand.AttachCamera: AttachCameraCommand(); break;
                case ScriptCommand.Delay: DelayCommand(); break;
                case ScriptCommand.SetFlag: SetFlagCommand(); break;
                case ScriptCommand.ClearFlag: ClearFlagCommand(); break;
                case ScriptCommand.Warp: WarpCommand(); break;
                case ScriptCommand.Message: MessageCommand(); break;
                case ScriptCommand.AwaitMessage: AwaitMessageCommand(); break;
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
            Game.Instance.Save.PlayerParty.HealFully();
        }

        private void GivePokemonCommand()
        {
            PBESpecies species = _reader.ReadEnum<PBESpecies>();
            byte level = _reader.ReadByte();
            var pkmn = PartyPokemon.GetTestPokemon(species, 0, level);
            Game.Instance.Save.GivePokemon(pkmn);
        }

        private void GivePokemonFormCommand()
        {
            PBESpecies species = _reader.ReadEnum<PBESpecies>();
            PBEForm form = _reader.ReadEnum<PBEForm>();
            byte level = _reader.ReadByte();
            var pkmn = PartyPokemon.GetTestPokemon(species, form, level);
            Game.Instance.Save.GivePokemon(pkmn);
        }

        private void GivePokemonFormItemCommand()
        {
            PBESpecies species = _reader.ReadEnum<PBESpecies>();
            PBEForm form = _reader.ReadEnum<PBEForm>();
            byte level = _reader.ReadByte();
            PBEItem item = _reader.ReadEnum<PBEItem>();
            var pkmn = PartyPokemon.GetTestPokemon(species, form, level);
            pkmn.Item = item;
            Game.Instance.Save.GivePokemon(pkmn);
        }

        private void MoveObjCommand()
        {
            ushort id = _reader.ReadUInt16();
            uint offset = _reader.ReadUInt32();
            long returnOffset = _reader.BaseStream.Position;
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
            _reader.BaseStream.Position = returnOffset;
            obj.RunNextScriptMovement();
        }

        private void AwaitObjMovementCommand()
        {
            ushort id = _reader.ReadUInt16();
            var obj = Obj.GetObj(id);
            _waitMovementObj = obj;
        }

        private void DetachCameraCommand()
        {
            CameraObj.CameraAttachedTo = null;
        }

        private void AttachCameraCommand()
        {
            ushort id = _reader.ReadUInt16();
            var obj = Obj.GetObj(id);
            CameraObj.CameraAttachedTo = obj;
            CameraObj.CameraCopyMovement();
        }

        private void DelayCommand()
        {
            ushort delay = _reader.ReadUInt16();
            _delay = delay;
        }

        private void SetFlagCommand()
        {
            Flag flag = _reader.ReadEnum<Flag>();
            Game.Instance.Save.Flags[flag] = true;
        }

        private void ClearFlagCommand()
        {
            Flag flag = _reader.ReadEnum<Flag>();
            Game.Instance.Save.Flags[flag] = false;
        }

        private void WarpCommand()
        {
            int mapId = _reader.ReadInt32();
            int x = _reader.ReadInt32();
            int y = _reader.ReadInt32();
            byte elevation = _reader.ReadByte();
            Game.Instance.TempWarp(new Warp(mapId, x, y, elevation));
        }

        private void MessageCommand()
        {
            uint textOffset = _reader.ReadUInt32();
            long returnOffset = _reader.BaseStream.Position;
            string text = _reader.ReadStringNullTerminated(textOffset);
            _reader.BaseStream.Position = returnOffset;
            Game.Instance.MessageBoxes.Add(new MessageBox(text));
        }

        private void AwaitMessageCommand()
        {
            _waitMessageBox = Game.Instance.MessageBoxes[Game.Instance.MessageBoxes.Count - 1];
        }
    }
}
