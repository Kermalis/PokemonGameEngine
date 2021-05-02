using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using System;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class Save
    {
        public GameStats GameStats { get; }
        public Pokedex Pokedex { get; }
        public Flags Flags { get; }
        public Vars Vars { get; }
        public OTInfo OT { get; }
        public PCBoxes PCBoxes { get; }
        public Daycare Daycare { get; }
        public Party PlayerParty { get; }
        public PlayerInventory PlayerInventory { get; }
        public uint Money { get; private set; }

        public Save()
        {
            GameStats = new GameStats();
            Pokedex = new Pokedex();
            Flags = new Flags();
            Vars = new Vars();
            OT = new OTInfo("Dawn", true);
            PCBoxes = new PCBoxes();
            Daycare = new Daycare();
            PlayerParty = new Party();
            {
                var victini = new PartyPokemon(PBESpecies.Victini, 0, 67, OT);
                victini.Ability = PBEAbility.Compoundeyes;
                victini.Item = PBEItem.Leftovers;
                victini.Status1 = PokemonBattleEngine.Battle.PBEStatus1.BadlyPoisoned;
                victini.Moveset[0].Move = PBEMove.Bounce;
                victini.Moveset[1].Move = PBEMove.ZenHeadbutt;
                victini.Moveset[2].Move = PBEMove.Surf;
                victini.Moveset[3].Move = PBEMove.VCreate;
                GivePokemon(victini);
            }
            for (int i = 0; i < 44; i++)
            {
                Debug_GiveRandomPokemon(i == 0 || i == 35);
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

        private void Debug_GiveRandomPokemon(bool egg)
        {
            (PBESpecies species, PBEForm form) = PBEDataProvider.GlobalRandom.RandomSpecies(true);
            byte level = egg ? (byte)1 : (byte)PBEDataProvider.GlobalRandom.RandomInt(PkmnConstants.MinLevel, PkmnConstants.MaxLevel);
            var pkmn = new PartyPokemon(species, form, level, OT);
            if (egg)
            {
                pkmn.Nickname = "Egg";
                pkmn.IsEgg = true;
            }
            else
            {
                pkmn.Item = PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.GetValidItems(pkmn.Species, pkmn.Form));
                pkmn.Debug_RandomizeMoves();
            }
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
