using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;
using System;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class Save
    {
        public GameStats GameStats { get; private set; }
        public Pokedex Pokedex { get; private set; }
        public Flags Flags { get; private set; }
        public Vars Vars { get; private set; }
        public OTInfo OT { get; private set; }
        public PCBoxes PCBoxes { get; private set; }
        public Daycare Daycare { get; private set; }
        public Party PlayerParty { get; private set; }
        public PlayerInventory PlayerInventory { get; private set; }
        public uint Money { get; private set; }

        private void InitPlayerWithDefaultLocation()
        {
            var map = Map.LoadOrGet(0);
            PlayerObj.Init(2, 29, map);
            CameraObj.Init();
        }
        public void Debug_Create()
        {
            GameStats = new GameStats();
            Pokedex = new Pokedex();
            Flags = new Flags();
            Vars = new Vars();
            OT = new OTInfo("Dawn", true);
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
            PlayerInventory.Add((PBEItem)631); // Oval charm and shiny charm
            PlayerInventory.Add((PBEItem)632);
            Money = 473_123;
            InitPlayerWithDefaultLocation();
            PCBoxes = new PCBoxes();
            Daycare = new Daycare();
            Daycare.StorePokemon(PartyPokemon.CreatePlayerOwnedMon(PBESpecies.Blissey, 0, 100));
            Daycare.StorePokemon(PartyPokemon.CreatePlayerOwnedMon(PBESpecies.Ditto, 0, 100));
            PlayerParty = new Party();
            {
                var victini = PartyPokemon.CreatePlayerOwnedMon(PBESpecies.Victini, 0, 67);
                victini.Ability = PBEAbility.Compoundeyes;
                victini.Item = PBEItem.Leftovers;
                victini.Status1 = PBEStatus1.BadlyPoisoned;
                victini.Moveset[0].Move = PBEMove.Bounce;
                victini.Moveset[1].Move = PBEMove.ZenHeadbutt;
                victini.Moveset[2].Move = PBEMove.Surf;
                victini.Moveset[3].Move = PBEMove.VCreate;
                GivePokemon(victini);
            }
            for (int i = 0; i < 44; i++)
            {
                Debug_GiveRandomPokemon(i == 0 || i == 1 || i == 35);
            }
        }

        private void Debug_GiveRandomPokemon(bool egg)
        {
            (PBESpecies species, PBEForm form) = PBEDataProvider.GlobalRandom.RandomSpecies(true);
            PartyPokemon pkmn;
            if (egg)
            {
                pkmn = PartyPokemon.CreateDefaultEgg(species, form);
            }
            else
            {
                byte level = (byte)PBEDataProvider.GlobalRandom.RandomInt(PkmnConstants.MinLevel, PkmnConstants.MaxLevel);
                pkmn = PartyPokemon.CreatePlayerOwnedMon(species, form, level);
                pkmn.Item = PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.GetValidItems(pkmn.Species, pkmn.Form));
                pkmn.Debug_RandomizeMoves();
            }
            GivePokemon(pkmn);
        }

        public void GivePokemon(PartyPokemon pkmn)
        {
            Pokedex.SetCaught(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny, pkmn.PID);
            // Try to add to party first, then pc boxes
            if (PlayerParty.Add(pkmn) == -1 && PCBoxes.Add(pkmn) == -1)
            {
                throw new Exception();
            }
        }
    }
}
