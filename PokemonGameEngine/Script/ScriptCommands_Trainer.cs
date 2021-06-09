using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Trainer;

namespace Kermalis.PokemonGameEngine.Script
{
    // TODO: Player defeat
    internal sealed partial class ScriptContext
    {
        private bool IsTrainerDefeated(out Flag trainer)
        {
            trainer = ReadVarOrEnum<Flag>();
            return Game.Instance.Save.Flags[trainer];
        }

        private void OnTrainerIntroFinished_NoContinue(Flag trainer, uint defeatedTextOffset)
        {
            CloseMessageCommand();
            string defeatText = ReadString(defeatedTextOffset);
            TrainerCore.CreateTrainerBattle_1v1(trainer, defeatText);
            _waitReturnToField = true;
            _onWaitReturnToFieldFinished = () => OnTrainerBattleFinished_NoContinue(trainer);
        }
        private void OnTrainerBattleFinished_NoContinue(Flag trainer)
        {
            Game.Instance.Save.Flags[trainer] = true;
            SetAllLock(false);
            Dispose();
        }

        private void TrainerBattle_Single_NoContinueCommand()
        {
            bool defeated = IsTrainerDefeated(out Flag trainer);
            uint introTextOffset = _reader.ReadUInt32();
            uint defeatedTextOffset = _reader.ReadUInt32();
            if (defeated)
            {
                return;
            }
            SetAllLock(true);
            string introText = ReadString(introTextOffset);
            CreateMessageBox(introText);
            AwaitMessageCommand(true);
            _onWaitMessageFinished = () => OnTrainerIntroFinished_NoContinue(trainer, defeatedTextOffset);
        }
    }
}
