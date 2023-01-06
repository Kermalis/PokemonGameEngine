using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Trainer;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;
using System;

namespace Kermalis.PokemonGameEngine.Script;

// TODO: Player defeat
// TODO: Encounter music
internal sealed partial class ScriptContext
{
	private bool IsTrainerDefeated(out Flag trainer)
	{
		trainer = ReadVarOrEnum<Flag>();
		return Game.Instance.Save.Flags[trainer];
	}
	private static void DisableEventObjMovement()
	{
		ushort id = (ushort)Game.Instance.Save.Vars[Var.LastTalked];
		if (id == Overworld.PlayerId)
		{
			return;
		}
		if (Obj.GetObj(id) is not EventObj e)
		{
			return;
		}
		e.MovementType = ObjMovementType.None;
	}

	private void OnTrainerIntroFinished_1v1(Flag trainer, uint defeatedTextOffset, Action onWin)
	{
		CloseMessageCommand();
		string defeatText = ReadString(defeatedTextOffset);
		TrainerCore.CreateTrainerBattle_1v1(trainer, defeatText);
		_waitReturnToField = true;
		_onWaitReturnToFieldFinished = onWin;
	}
	private void OnTrainerBattleFinished_NoContinue(Flag trainer)
	{
		Game.Instance.Save.Flags[trainer] = true;
		DisableEventObjMovement();
		Obj.SetAllLock(false);
		Delete();
	}
	private void OnTrainerBattleFinished_Continue(Flag trainer, uint continueOffset)
	{
		Game.Instance.Save.Flags[trainer] = true;
		DisableEventObjMovement();
		_reader.BaseStream.Position = continueOffset;
	}

	private void TrainerBattleCommand()
	{
		bool defeated = IsTrainerDefeated(out Flag trainer);
		uint introTextOffset = _reader.ReadUInt32();
		uint defeatedTextOffset = _reader.ReadUInt32();
		if (defeated)
		{
			return;
		}
		Obj.SetAllLock(true);
		Obj.FaceLastTalkedTowardsPlayer();
		string introText = ReadString(introTextOffset);
		CreateMessageBox(introText);
		AwaitMessageCommand(true);
		_onWaitMessageFinished = () => OnTrainerIntroFinished_1v1(trainer, defeatedTextOffset, () => OnTrainerBattleFinished_NoContinue(trainer));
	}
	private void TrainerBattle_ContinueCommand()
	{
		bool defeated = IsTrainerDefeated(out Flag trainer);
		uint introTextOffset = _reader.ReadUInt32();
		uint defeatedTextOffset = _reader.ReadUInt32();
		uint continueOffset = _reader.ReadUInt32();
		if (defeated)
		{
			return;
		}
		Obj.SetAllLock(true);
		Obj.FaceLastTalkedTowardsPlayer();
		string introText = ReadString(introTextOffset);
		CreateMessageBox(introText);
		AwaitMessageCommand(true);
		_onWaitMessageFinished = () => OnTrainerIntroFinished_1v1(trainer, defeatedTextOffset, () => OnTrainerBattleFinished_Continue(trainer, continueOffset));
	}
}
