using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.Pkmn;
using Kermalis.PokemonGameEngine.Render.World;

namespace Kermalis.PokemonGameEngine.Script
{
    internal sealed partial class ScriptContext
    {
        private static void GetDaycareStateCommand()
        {
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = (byte)Game.Instance.Save.Daycare.GetDaycareState();
        }
        private void BufferDaycareMonNicknameCommand()
        {
            byte buf = (byte)ReadVarOrValue();
            byte index = (byte)ReadVarOrValue();
            string nickname = Game.Instance.Save.Daycare.GetNickname(index);
            Game.Instance.StringBuffers.Buffers[buf] = nickname;
        }
        private static void StorePokemonInDaycareCommand()
        {
            int index = Game.Instance.Save.Vars[Var.SpecialVar_Result];
            PartyPokemon pkmn = Game.Instance.Save.PlayerParty[index];
            Game.Instance.Save.PlayerParty.Remove(pkmn);
            Game.Instance.Save.Daycare.StorePokemon(pkmn);
            Game.Instance.StringBuffers.Buffers[0] = pkmn.Nickname;
        }
        private static void GetDaycareCompatibilityCommand()
        {
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = Game.Instance.Save.Daycare.GetCompatibility();
        }
        private void SelectDaycareMonCommand()
        {
            OverworldGUI.Instance.OpenPartyMenu(PartyGUI.Mode.SelectDaycare);
            _waitReturnToField = true;
        }
        private void GetDaycareMonLevelsGainedCommand()
        {
            byte index = (byte)ReadVarOrValue();
            byte gained = Game.Instance.Save.Daycare.GetNumLevelsGained(index);
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = gained;
            if (gained != 0)
            {
                Game.Instance.StringBuffers.Buffers[1] = string.Format("{0} level{1}", gained, gained == 1 ? string.Empty : 's');
            }
        }
        private static void GiveDaycareEggCommand()
        {
            Game.Instance.Save.Daycare.GiveEgg();
        }
        private static void DisposeDaycareEggCommand()
        {
            Game.Instance.Save.Daycare.DisposeEgg();
        }
        private static void HatchEggCommand()
        {
            OverworldGUI.Instance.StartEggHatchScreen();
        }
    }
}
