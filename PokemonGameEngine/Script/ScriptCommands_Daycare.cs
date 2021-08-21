using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.GUI.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn;

namespace Kermalis.PokemonGameEngine.Script
{
    internal sealed partial class ScriptContext
    {
        private static void GetDaycareStateCommand()
        {
            Engine.Instance.Save.Vars[Var.SpecialVar_Result] = (byte)Engine.Instance.Save.Daycare.GetDaycareState();
        }
        private void BufferDaycareMonNicknameCommand()
        {
            byte buf = (byte)ReadVarOrValue();
            byte index = (byte)ReadVarOrValue();
            string nickname = Engine.Instance.Save.Daycare.GetNickname(index);
            Engine.Instance.StringBuffers.Buffers[buf] = nickname;
        }
        private static void StorePokemonInDaycareCommand()
        {
            int index = Engine.Instance.Save.Vars[Var.SpecialVar_Result];
            PartyPokemon pkmn = Engine.Instance.Save.PlayerParty[index];
            Engine.Instance.Save.PlayerParty.Remove(pkmn);
            Engine.Instance.Save.Daycare.StorePokemon(pkmn);
            Engine.Instance.StringBuffers.Buffers[0] = pkmn.Nickname;
        }
        private static void GetDaycareCompatibilityCommand()
        {
            Engine.Instance.Save.Vars[Var.SpecialVar_Result] = Engine.Instance.Save.Daycare.GetCompatibility();
        }
        private void SelectDaycareMonCommand()
        {
            OverworldGUI.Instance.OpenPartyMenu(PartyGUI.Mode.SelectDaycare);
            _waitReturnToField = true;
        }
        private void GetDaycareMonLevelsGainedCommand()
        {
            byte index = (byte)ReadVarOrValue();
            byte gained = Engine.Instance.Save.Daycare.GetNumLevelsGained(index);
            Engine.Instance.Save.Vars[Var.SpecialVar_Result] = gained;
            if (gained != 0)
            {
                Engine.Instance.StringBuffers.Buffers[1] = string.Format("{0} level{1}", gained, gained == 1 ? string.Empty : 's');
            }
        }
        private static void GiveDaycareEggCommand()
        {
            Engine.Instance.Save.Daycare.GiveEgg();
        }
        private static void DisposeDaycareEggCommand()
        {
            Engine.Instance.Save.Daycare.DisposeEgg();
        }
        private static void HatchEggCommand()
        {
            OverworldGUI.Instance.StartEggHatchScreen();
        }
    }
}
