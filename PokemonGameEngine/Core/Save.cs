using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class Save
    {
        public Pokedex Pokedex { get; }
        public Flags Flags { get; }
        public Vars Vars { get; }
        public string PlayerName { get; }
        public bool PlayerIsFemale { get; }
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
            PlayerParty = new Party();
            {
                var victini = new PartyPokemon(PBESpecies.Victini, 0, 67);
                victini.Ability = PBEAbility.Compoundeyes;
                victini.Item = PBEItem.Leftovers;
                victini.Moveset[0].Move = PBEMove.Bounce;
                victini.Moveset[1].Move = PBEMove.ZenHeadbutt;
                victini.Moveset[2].Move = PBEMove.StunSpore;
                victini.Moveset[3].Move = PBEMove.VCreate;
                GivePokemon(victini);
            }
            for (int i = 0; i < PBESettings.DefaultMaxPartySize - 1; i++)
            {
                Test_GiveRandomPokemon();
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

        private void Test_GiveRandomPokemon()
        {
            (PBESpecies species, PBEForm form) = PBEDataProvider.GlobalRandom.RandomSpecies(true);
            var pkmn = new PartyPokemon(species, form, (byte)PBEDataProvider.GlobalRandom.RandomInt(PBESettings.DefaultMinLevel, PBESettings.DefaultMaxLevel));
            pkmn.RandomizeMoves();
            GivePokemon(pkmn);
        }

        // TODO: If party is full, send to a box, if boxes are full, error
        public void GivePokemon(PartyPokemon pkmn)
        {
            Pokedex.SetCaught(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.PID);
            PlayerParty.Add(pkmn);
        }
    }
}
