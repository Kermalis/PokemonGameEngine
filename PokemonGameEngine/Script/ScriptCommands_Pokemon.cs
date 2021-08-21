using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.World;

namespace Kermalis.PokemonGameEngine.Script
{
    internal sealed partial class ScriptContext
    {
        private void OnCryFinished(SoundChannel _)
        {
            _waitCry = false;
        }

        private static void HealPartyCommand()
        {
            Engine.Instance.Save.PlayerParty.HealFully();
        }

        private void GivePokemonCommand()
        {
            PBESpecies species = ReadVarOrEnum<PBESpecies>();
            byte level = (byte)ReadVarOrValue();
            var pkmn = PartyPokemon.CreatePlayerOwnedMon(species, 0, level);
            Engine.Instance.Save.GivePokemon(pkmn);
        }
        private void GivePokemonFormCommand()
        {
            PBESpecies species = ReadVarOrEnum<PBESpecies>();
            PBEForm form = ReadVarOrEnum<PBEForm>();
            byte level = (byte)ReadVarOrValue();
            var pkmn = PartyPokemon.CreatePlayerOwnedMon(species, form, level);
            Engine.Instance.Save.GivePokemon(pkmn);
        }
        private void GivePokemonFormItemCommand()
        {
            PBESpecies species = ReadVarOrEnum<PBESpecies>();
            PBEForm form = ReadVarOrEnum<PBEForm>();
            byte level = (byte)ReadVarOrValue();
            ItemType item = ReadVarOrEnum<ItemType>();
            var pkmn = PartyPokemon.CreatePlayerOwnedMon(species, form, level);
            pkmn.Item = item;
            Engine.Instance.Save.GivePokemon(pkmn);
        }

        private void BufferSpeciesNameCommand()
        {
            byte buffer = (byte)ReadVarOrValue();
            PBESpecies species = ReadVarOrEnum<PBESpecies>();
            Engine.Instance.StringBuffers.Buffers[buffer] = PBEDataProvider.Instance.GetSpeciesName(species).English;
        }
        private void BufferPartyMonNicknameCommand()
        {
            byte buffer = (byte)ReadVarOrValue();
            byte index = (byte)ReadVarOrValue();
            Engine.Instance.StringBuffers.Buffers[buffer] = Engine.Instance.Save.PlayerParty[index].Nickname;
        }

        private void PlayCryCommand()
        {
            PBESpecies species = ReadVarOrEnum<PBESpecies>();
            PBEForm form = ReadVarOrEnum<PBEForm>();
            SoundControl.PlayCry(species, form, onStopped: OnCryFinished);
        }
        private void AwaitCryCommand()
        {
            _waitCry = true;
        }

        private static void CountNonEggPartyCommand()
        {
            short count = 0;
            foreach (PartyPokemon p in Engine.Instance.Save.PlayerParty)
            {
                if (!p.IsEgg)
                {
                    count++;
                }
            }
            Engine.Instance.Save.Vars[Var.SpecialVar_Result] = count;
        }
        private static void CountNonFaintedNonEggPartyCommand()
        {
            short count = 0;
            foreach (PartyPokemon p in Engine.Instance.Save.PlayerParty)
            {
                if (!p.IsEgg && p.HP > 0)
                {
                    count++;
                }
            }
            Engine.Instance.Save.Vars[Var.SpecialVar_Result] = count;
        }
        private static void CountPlayerPartyCommand()
        {
            Engine.Instance.Save.Vars[Var.SpecialVar_Result] = (short)Engine.Instance.Save.PlayerParty.Count;
        }

        private void CheckPartyHasMoveCommand()
        {
            PBEMove move = ReadVarOrEnum<PBEMove>();
            Overworld.GetNonEggPartyMonWithMove(move, out _, out int index);
            Engine.Instance.Save.Vars[Var.SpecialVar_Result] = (short)index;
        }
    }
}
