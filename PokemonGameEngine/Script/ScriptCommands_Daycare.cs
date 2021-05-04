using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.GUI.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn;

namespace Kermalis.PokemonGameEngine.Script
{
    internal sealed partial class ScriptContext
    {
        private void GetDaycareStateCommand()
        {
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = (byte)Game.Instance.Save.Daycare.GetDaycareState();
        }
        private void StorePokemonInDaycareCommand()
        {
            int index = Game.Instance.Save.Vars[Var.SpecialVar_Result];
            PartyPokemon pkmn = Game.Instance.Save.PlayerParty[index];
            Game.Instance.Save.PlayerParty.Remove(pkmn);
            Game.Instance.Save.Daycare.StorePokemon(pkmn);
            Game.Instance.StringBuffers.Buffers[0] = pkmn.Nickname;
        }
        private void GetDaycareCompatibilityCommand()
        {
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = Game.Instance.Save.Daycare.GetCompatibility();
        }
        private void SelectDaycareMonCommand()
        {
            OverworldGUI.Instance.OpenPartyMenu(PartyGUI.Mode.SelectDaycare);
            _waitReturnToField = true;
        }
        private void GiveDaycareEggCommand()
        {
            Game.Instance.Save.Daycare.GiveEgg();
        }
        private void DisposeDaycareEggCommand()
        {
            Game.Instance.Save.Daycare.DisposeEgg();
        }
    }
}
