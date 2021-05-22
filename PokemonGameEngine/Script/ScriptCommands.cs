using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Scripts;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
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
                case ScriptCommand.AwaitMessageRead: AwaitMessageCommand(false); break;
                case ScriptCommand.AwaitMessageComplete: AwaitMessageCommand(true); break;
                case ScriptCommand.LockObj: LockObjCommand(); break;
                case ScriptCommand.LockAllObjs: LockAllObjsCommand(); break;
                case ScriptCommand.UnlockObj: UnlockObjCommand(); break;
                case ScriptCommand.UnlockAllObjs: UnlockAllObjsCommand(); break;
                case ScriptCommand.SetVar: SetVarCommand(); break;
                case ScriptCommand.AddVar: AddVarCommand(); break;
                case ScriptCommand.SubVar: SubVarCommand(); break;
                case ScriptCommand.MulVar: MulVarCommand(); break;
                case ScriptCommand.DivVar: DivVarCommand(); break;
                case ScriptCommand.RshftVar: RshiftVarCommand(); break;
                case ScriptCommand.LshiftVar: LshiftVarCommand(); break;
                case ScriptCommand.AndVar: AndVarCommand(); break;
                case ScriptCommand.OrVar: OrVarCommand(); break;
                case ScriptCommand.XorVar: XorVarCommand(); break;
                case ScriptCommand.RandomizeVar: RandomizeVarCommand(); break;
                case ScriptCommand.GoToIf: GoToIfCommand(); break;
                case ScriptCommand.GoToIfFlag: GoToIfFlagCommand(); break;
                case ScriptCommand.CallIf: CallIfCommand(); break;
                case ScriptCommand.CallIfFlag: CallIfFlagCommand(); break;
                case ScriptCommand.BufferSpeciesName: BufferSpeciesNameCommand(); break;
                case ScriptCommand.WildBattle: WildBattleCommand(); break;
                case ScriptCommand.AwaitReturnToField: AwaitReturnToFieldCommand(); break;
                case ScriptCommand.CloseMessage: CloseMessageCommand(); break;
                case ScriptCommand.UnloadObj: UnloadObjCommand(); break;
                case ScriptCommand.LookTowardsObj: LookTowardsObjCommand(); break;
                case ScriptCommand.BufferSeenCount: BufferSeenCountCommand(); break;
                case ScriptCommand.BufferCaughtCount: BufferCaughtCountCommand(); break;
                case ScriptCommand.GetDaycareState: GetDaycareStateCommand(); break;
                case ScriptCommand.StorePokemonInDaycare: StorePokemonInDaycareCommand(); break;
                case ScriptCommand.GetDaycareCompatibility: GetDaycareCompatibilityCommand(); break;
                case ScriptCommand.SelectDaycareMon: SelectDaycareMonCommand(); break;
                case ScriptCommand.GiveDaycareEgg: GiveDaycareEggCommand(); break;
                case ScriptCommand.DisposeDaycareEgg: DisposeDaycareEggCommand(); break;
                case ScriptCommand.HatchEgg: HatchEggCommand(); break;
                case ScriptCommand.YesNoChoice: YesNoChoiceCommand(); break;
                case ScriptCommand.IncrementGameStat: IncrementGameStatCommand(); break;
                case ScriptCommand.PlayCry: PlayCryCommand(); break;
                case ScriptCommand.CountNonEggParty: CountNonEggPartyCommand(); break;
                case ScriptCommand.CountNonFaintedNonEggParty: CountNonFaintedNonEggPartyCommand(); break;
                case ScriptCommand.CountPlayerParty: CountPlayerPartyCommand(); break;
                default: throw new InvalidDataException();
            }
        }

        // Regular "Var" enum is not handled here, use _reader.ReadEnum<Var>() instead
        private TEnum ReadVarOrEnum<TEnum>() where TEnum : struct, Enum
        {
            Type enumType = typeof(TEnum);
            Type underlyingType = Enum.GetUnderlyingType(enumType);
            switch (underlyingType.FullName)
            {
                case "System.Byte":
                case "System.SByte":
                case "System.Int16":
                case "System.UInt16": return (TEnum)Enum.ToObject(enumType, ReadVarOrValue());
                default: return _reader.ReadEnum<TEnum>();
            }
        }
        private short ReadVarOrValue()
        {
            return Game.Instance.Save.Vars.GetVarOrValue(_reader.ReadUInt32());
        }

        private uint? IfVar()
        {
            uint offset = _reader.ReadUInt32();
            short value1 = ReadVarOrValue();
            ScriptConditional cond = ReadVarOrEnum<ScriptConditional>();
            short value2 = ReadVarOrValue();
            return cond.Match(value1, value2) ? offset : null;
        }
        private uint? IfFlag()
        {
            uint offset = _reader.ReadUInt32();
            Flag flag = ReadVarOrEnum<Flag>();
            byte value = (byte)ReadVarOrValue();
            if (Game.Instance.Save.Flags[flag] ? value != 0 : value == 0)
            {
                return offset;
            }
            return null;
        }

        private void PushPosition(uint newOffset)
        {
            _callStack.Push(_reader.BaseStream.Position);
            _reader.BaseStream.Position = newOffset;
        }
        private void PopPosition()
        {
            _reader.BaseStream.Position = _callStack.Pop();
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
            PushPosition(offset);
        }
        private void ReturnCommand()
        {
            PopPosition();
        }
        private void GoToIfCommand()
        {
            uint? offset = IfVar();
            if (offset.HasValue)
            {
                _reader.BaseStream.Position = offset.Value;
            }
        }
        private void GoToIfFlagCommand()
        {
            uint? offset = IfFlag();
            if (offset.HasValue)
            {
                _reader.BaseStream.Position = offset.Value;
            }
        }
        private void CallIfCommand()
        {
            uint? offset = IfVar();
            if (offset.HasValue)
            {
                PushPosition(offset.Value);
            }
        }
        private void CallIfFlagCommand()
        {
            uint? offset = IfFlag();
            if (offset.HasValue)
            {
                PushPosition(offset.Value);
            }
        }

        private static void HealPartyCommand()
        {
            Game.Instance.Save.PlayerParty.HealFully();
        }

        private void GivePokemonCommand()
        {
            PBESpecies species = ReadVarOrEnum<PBESpecies>();
            byte level = (byte)ReadVarOrValue();
            var pkmn = PartyPokemon.CreatePlayerOwnedMon(species, 0, level);
            Game.Instance.Save.GivePokemon(pkmn);
        }
        private void GivePokemonFormCommand()
        {
            PBESpecies species = ReadVarOrEnum<PBESpecies>();
            PBEForm form = ReadVarOrEnum<PBEForm>();
            byte level = (byte)ReadVarOrValue();
            var pkmn = PartyPokemon.CreatePlayerOwnedMon(species, form, level);
            Game.Instance.Save.GivePokemon(pkmn);
        }
        private void GivePokemonFormItemCommand()
        {
            PBESpecies species = ReadVarOrEnum<PBESpecies>();
            PBEForm form = ReadVarOrEnum<PBEForm>();
            byte level = (byte)ReadVarOrValue();
            ItemType item = ReadVarOrEnum<ItemType>();
            var pkmn = PartyPokemon.CreatePlayerOwnedMon(species, form, level);
            pkmn.Item = item;
            Game.Instance.Save.GivePokemon(pkmn);
        }

        private void MoveObjCommand()
        {
            ushort id = (ushort)ReadVarOrValue();
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
            obj.IsScriptMoving = true;
        }
        private void UnloadObjCommand()
        {
            ushort id = (ushort)ReadVarOrValue();
            var obj = Obj.GetObj(id);
            Obj.LoadedObjs.Remove(obj);
            obj.Map.Objs.Remove(obj);
        }
        private void AwaitObjMovementCommand()
        {
            ushort id = (ushort)ReadVarOrValue();
            var obj = Obj.GetObj(id);
            _waitMovementObj = obj;
        }
        private void LookTowardsObjCommand()
        {
            ushort id1 = (ushort)ReadVarOrValue();
            ushort id2 = (ushort)ReadVarOrValue();
            var looker = Obj.GetObj(id1);
            var target = Obj.GetObj(id2);
            looker.LookTowards(target);
        }

        private static void DetachCameraCommand()
        {
            CameraObj.CameraAttachedTo = null;
            // Camera should probably have properties that get its attachment or its own properties
            // Instead of using CameraCopyMovement()
            // Map changing will be tougher though
            //CameraObj.Camera.IsScriptMoving = false;
        }
        private void AttachCameraCommand()
        {
            ushort id = (ushort)ReadVarOrValue();
            var obj = Obj.GetObj(id);
            CameraObj.CameraAttachedTo = obj;
            CameraObj.CameraCopyMovement();
        }

        private void DelayCommand()
        {
            ushort delay = (ushort)ReadVarOrValue();
            _delay = delay;
        }

        private void SetFlagCommand()
        {
            Flag flag = ReadVarOrEnum<Flag>();
            Game.Instance.Save.Flags[flag] = true;
        }
        private void ClearFlagCommand()
        {
            Flag flag = ReadVarOrEnum<Flag>();
            Game.Instance.Save.Flags[flag] = false;
        }

        private void WarpCommand()
        {
            int mapId = _reader.ReadInt32();
            int x = _reader.ReadInt32();
            int y = _reader.ReadInt32();
            byte elevation = (byte)ReadVarOrValue();
            OverworldGUI.Instance.TempWarp(new Warp(mapId, x, y, elevation));
        }

        private void SetLock(bool locked)
        {
            ushort id = (ushort)ReadVarOrValue();
            var obj = Obj.GetObj(id);
            obj.IsLocked = locked;
        }
        private void LockObjCommand()
        {
            SetLock(true);
        }
        private void UnlockObjCommand()
        {
            SetLock(false);
        }

        private static void SetAllLock(bool locked)
        {
            foreach (Obj o in Obj.LoadedObjs)
            {
                o.IsLocked = locked;
            }
        }
        private static void LockAllObjsCommand()
        {
            SetAllLock(true);
        }
        private static void UnlockAllObjsCommand()
        {
            SetAllLock(false);
        }

        private void SetVarCommand()
        {
            Var var = _reader.ReadEnum<Var>();
            short value = ReadVarOrValue();
            Game.Instance.Save.Vars[var] = value;
        }
        private void AddVarCommand()
        {
            Var var = _reader.ReadEnum<Var>();
            short value = ReadVarOrValue();
            Game.Instance.Save.Vars[var] += value;
        }
        private void SubVarCommand()
        {
            Var var = _reader.ReadEnum<Var>();
            short value = ReadVarOrValue();
            Game.Instance.Save.Vars[var] -= value;
        }
        private void MulVarCommand()
        {
            Var var = _reader.ReadEnum<Var>();
            short value = ReadVarOrValue();
            Game.Instance.Save.Vars[var] *= value;
        }
        private void DivVarCommand()
        {
            Var var = _reader.ReadEnum<Var>();
            short value = ReadVarOrValue();
            Game.Instance.Save.Vars[var] /= value;
        }
        private void RshiftVarCommand()
        {
            Var var = _reader.ReadEnum<Var>();
            short value = ReadVarOrValue();
            Game.Instance.Save.Vars[var] >>= value;
        }
        private void LshiftVarCommand()
        {
            Var var = _reader.ReadEnum<Var>();
            short value = ReadVarOrValue();
            Game.Instance.Save.Vars[var] <<= value;
        }
        private void AndVarCommand()
        {
            Var var = _reader.ReadEnum<Var>();
            short value = ReadVarOrValue();
            Game.Instance.Save.Vars[var] &= value;
        }
        private void OrVarCommand()
        {
            Var var = _reader.ReadEnum<Var>();
            short value = ReadVarOrValue();
            Game.Instance.Save.Vars[var] |= value;
        }
        private void XorVarCommand()
        {
            Var var = _reader.ReadEnum<Var>();
            short value = ReadVarOrValue();
            Game.Instance.Save.Vars[var] ^= value;
        }
        private void RandomizeVarCommand()
        {
            Var var = _reader.ReadEnum<Var>();
            short min = ReadVarOrValue();
            short max = ReadVarOrValue();
            short value = (short)PBEDataProvider.GlobalRandom.RandomInt(min, max);
            Game.Instance.Save.Vars[var] = value;
        }

        private void BufferSpeciesNameCommand()
        {
            byte buffer = (byte)ReadVarOrValue();
            PBESpecies species = ReadVarOrEnum<PBESpecies>();
            Game.Instance.StringBuffers.Buffers[buffer] = PBEDataProvider.Instance.GetSpeciesName(species).English;
        }
        private void BufferSeenCountCommand()
        {
            byte buffer = (byte)ReadVarOrValue();
            Game.Instance.StringBuffers.Buffers[buffer] = Game.Instance.Save.Pokedex.GetSpeciesSeen().ToString();
        }
        private void BufferCaughtCountCommand()
        {
            byte buffer = (byte)ReadVarOrValue();
            Game.Instance.StringBuffers.Buffers[buffer] = Game.Instance.Save.Pokedex.GetSpeciesCaught().ToString();
        }

        private void WildBattleCommand()
        {
            PBESpecies species = ReadVarOrEnum<PBESpecies>();
            PBEForm form = ReadVarOrEnum<PBEForm>();
            byte level = (byte)ReadVarOrValue();
            var pkmn = PartyPokemon.CreateWildMon(species, form, level);
            Game.Instance.TempCreateWildBattle(pkmn);
        }
        private void AwaitReturnToFieldCommand()
        {
            _waitReturnToField = true;
        }

        private void IncrementGameStatCommand()
        {
            GameStat stat = ReadVarOrEnum<GameStat>();
            Game.Instance.Save.GameStats[stat]++;
        }

        private void PlayCryCommand()
        {
            PBESpecies species = ReadVarOrEnum<PBESpecies>();
            PBEForm form = ReadVarOrEnum<PBEForm>();
            SoundControl.Debug_PlayCry(species, form);
        }

        private static void CountNonEggPartyCommand()
        {
            short count = 0;
            foreach (PartyPokemon p in Game.Instance.Save.PlayerParty)
            {
                if (!p.IsEgg)
                {
                    count++;
                }
            }
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = count;
        }
        private static void CountNonFaintedNonEggPartyCommand()
        {
            short count = 0;
            foreach (PartyPokemon p in Game.Instance.Save.PlayerParty)
            {
                if (!p.IsEgg && p.HP > 0)
                {
                    count++;
                }
            }
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = count;
        }
        private static void CountPlayerPartyCommand()
        {
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = (short)Game.Instance.Save.PlayerParty.Count;
        }
    }
}
