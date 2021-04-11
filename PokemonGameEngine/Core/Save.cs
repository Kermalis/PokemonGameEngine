using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using System;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class Save
    {
        public Pokedex Pokedex { get; }
        public Flags Flags { get; }
        public Vars Vars { get; }
        public string PlayerName { get; }
        public bool PlayerIsFemale { get; }
        public PCBoxes PCBoxes { get; }
        public Party PlayerParty { get; }
        public PlayerInventory PlayerInventory { get; }
        public uint Money { get; private set; }

        public Save()
        {
            Pokedex = new Pokedex();
            Flags = new Flags();
            Vars = new Vars();
            PlayerName = "Dawn";
            PlayerIsFemale = true;
            PCBoxes = new PCBoxes();
            PlayerParty = new Party();
            {
                var victini = new PartyPokemon(PBESpecies.Victini, 0, 67);
                victini.Ability = PBEAbility.Compoundeyes;
                victini.Item = PBEItem.Leftovers;
                victini.Moveset[0].Move = PBEMove.Bounce;
                victini.Moveset[1].Move = PBEMove.ZenHeadbutt;
                victini.Moveset[2].Move = PBEMove.Surf;
                victini.Moveset[3].Move = PBEMove.VCreate;
                GivePokemon(victini);
            }
            for (int i = 0; i < 19; i++)
            {
                Debug_GiveRandomPokemon();
            }
            PlayerInventory = new PlayerInventory();
            PlayerInventory.Add(PBEItem.DuskBall, 995);
            PlayerInventory.Add(PBEItem.RockyHelmet, 42);
            PlayerInventory.Add(PBEItem.Leftovers, 473);
            PlayerInventory.Add(PBEItem.Potion, 123);
            PlayerInventory.Add(PBEItem.RedScarf, 230);
            PlayerInventory.Add(PBEItem.PokeDoll, 130);
            PlayerInventory.Add(PBEItem.XSpDef, 120);
            PlayerInventory.Add(PBEItem.AirBalloon, 407);
            PlayerInventory.Add(PBEItem.AdamantOrb, 73);
            PlayerInventory.Add(PBEItem.DarkGem, 69);
            PlayerInventory.Add(PBEItem.FluffyTail, 888);
            Money = 473_123;
        }

        private void Debug_GiveRandomPokemon()
        {
            (PBESpecies species, PBEForm form) = PBEDataProvider.GlobalRandom.RandomSpecies(true);
            var pkmn = new PartyPokemon(species, form, (byte)PBEDataProvider.GlobalRandom.RandomInt(PkmnConstants.MinLevel, PkmnConstants.MaxLevel));
            pkmn.Debug_RandomizeMoves();
            GivePokemon(pkmn);
        }

        public void GivePokemon(PartyPokemon pkmn)
        {
            Pokedex.SetCaught(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.PID);
            // Try to add to party first, then pc boxes
            if (PlayerParty.Add(pkmn) == -1 && PCBoxes.Add(pkmn) == -1)
            {
                throw new Exception();
            }
        }
    }
}
