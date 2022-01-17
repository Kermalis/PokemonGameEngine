using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Scripts;
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
                case ScriptCommand.End: Delete(); break;
                case ScriptCommand.GoTo: GoToCommand(); break;
                case ScriptCommand.Call: CallCommand(); break;
                case ScriptCommand.Return: PopPosition(); break;
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
                case ScriptCommand.MessageScale: MessageScaleCommand(); break;
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
                case ScriptCommand.BufferPartyMonNickname: BufferPartyMonNicknameCommand(); break;
                case ScriptCommand.WildBattle: WildBattleCommand(); break;
                case ScriptCommand.TrainerBattle: TrainerBattleCommand(); break;
                case ScriptCommand.TrainerBattle_Continue: TrainerBattle_ContinueCommand(); break;
                case ScriptCommand.AwaitReturnToField: AwaitReturnToFieldCommand(); break;
                case ScriptCommand.CloseMessage: CloseMessageCommand(); break;
                case ScriptCommand.UnloadObj: UnloadObjCommand(); break;
                case ScriptCommand.LookTowardsObj: LookTowardsObjCommand(); break;
                case ScriptCommand.LookLastTalkedTowardsPlayer: Obj.FaceLastTalkedTowardsPlayer(); break;
                case ScriptCommand.BufferSeenCount: BufferSeenCountCommand(); break;
                case ScriptCommand.BufferCaughtCount: BufferCaughtCountCommand(); break;
                case ScriptCommand.GetDaycareState: GetDaycareStateCommand(); break;
                case ScriptCommand.BufferDaycareMonNickname: BufferDaycareMonNicknameCommand(); break;
                case ScriptCommand.StorePokemonInDaycare: StorePokemonInDaycareCommand(); break;
                case ScriptCommand.GetDaycareCompatibility: GetDaycareCompatibilityCommand(); break;
                case ScriptCommand.SelectDaycareMon: SelectDaycareMonCommand(); break;
                case ScriptCommand.GetDaycareMonLevelsGained: GetDaycareMonLevelsGainedCommand(); break;
                case ScriptCommand.GiveDaycareEgg: GiveDaycareEggCommand(); break;
                case ScriptCommand.DisposeDaycareEgg: DisposeDaycareEggCommand(); break;
                case ScriptCommand.HatchEgg: HatchEggCommand(); break;
                case ScriptCommand.YesNoChoice: YesNoChoiceCommand(); break;
                case ScriptCommand.IncrementGameStat: IncrementGameStatCommand(); break;
                case ScriptCommand.PlayCry: PlayCryCommand(); break;
                case ScriptCommand.AwaitCry: AwaitCryCommand(); break;
                case ScriptCommand.CountNonEggParty: CountNonEggPartyCommand(); break;
                case ScriptCommand.CountNonFaintedNonEggParty: CountNonFaintedNonEggPartyCommand(); break;
                case ScriptCommand.CountPlayerParty: CountPlayerPartyCommand(); break;
                case ScriptCommand.CountBadges: CountBadgesCommand(); break;
                case ScriptCommand.BufferBadges: BufferBadgesCommand(); break;
                case ScriptCommand.CheckPartyHasMove: CheckPartyHasMoveCommand(); break;
                case ScriptCommand.UseSurf: UseSurfCommand(); break;
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
            return Game.Instance.Save.Vars.GetVarOrValue(_reader.ReadInt32());
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
        private void GoToIfCommand()
        {
            uint? offset = IfVar();
            if (offset is not null)
            {
                _reader.BaseStream.Position = offset.Value;
            }
        }
        private void GoToIfFlagCommand()
        {
            uint? offset = IfFlag();
            if (offset is not null)
            {
                _reader.BaseStream.Position = offset.Value;
            }
        }
        private void CallIfCommand()
        {
            uint? offset = IfVar();
            if (offset is not null)
            {
                PushPosition(offset.Value);
            }
        }
        private void CallIfFlagCommand()
        {
            uint? offset = IfFlag();
            if (offset is not null)
            {
                PushPosition(offset.Value);
            }
        }

        private void DelayCommand()
        {
            float delay = (float)_reader.ReadSingle();
            _delayRemaining = delay;
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
            EncounterMaker.CreateStaticWildBattle(species, form, level);
        }

        private void IncrementGameStatCommand()
        {
            GameStat stat = ReadVarOrEnum<GameStat>();
            Game.Instance.Save.GameStats[stat]++;
        }

        private static void CountBadgesCommand()
        {
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = (short)Game.Instance.Save.Flags.GetNumBadges();
        }
        private void BufferBadgesCommand()
        {
            byte buffer = (byte)ReadVarOrValue();
            Game.Instance.StringBuffers.Buffers[buffer] = Game.Instance.Save.Flags.GetNumBadges().ToString();
        }
    }
}
