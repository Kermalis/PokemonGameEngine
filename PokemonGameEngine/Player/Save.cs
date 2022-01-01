using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Maps;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
#if DEBUG_OVERWORLD
using Kermalis.PokemonGameEngine.Debug;
#endif

namespace Kermalis.PokemonGameEngine.Player
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

        private static void InitPlayerWithDefaultLocation()
        {
#if DEBUG_OVERWORLD
            Log.WriteLineWithTime("Initializing saved map, player obj, and camera obj");
            Log.ModifyIndent(+1);
#endif
            var map = Map.LoadOrGet(0);
            PlayerObj.Init(new WorldPos(new Pos2D(2, 29), 0), map, PlayerObjState.Walking);
            CameraObj.Init();
#if DEBUG_OVERWORLD
            Log.ModifyIndent(-1);
            Log.WriteLine("Done initializing saved map");
#endif
        }
        public void Debug_Create()
        {
            GameStats = new GameStats();
            Pokedex = new Pokedex();
            Flags = new Flags();
            Vars = new Vars();
            OT = new OTInfo("Dawn", true);
            PlayerInventory = new PlayerInventory();
            PlayerInventory.Add(ItemType.DuskBall, 995);
            PlayerInventory.Add(ItemType.RockyHelmet, 42);
            PlayerInventory.Add(ItemType.Leftovers, 473);
            PlayerInventory.Add(ItemType.Potion, 123);
            PlayerInventory.Add(ItemType.RedScarf, 230);
            PlayerInventory.Add(ItemType.PokeDoll, 130);
            PlayerInventory.Add(ItemType.XSpDef, 120);
            PlayerInventory.Add(ItemType.AirBalloon, 407);
            PlayerInventory.Add(ItemType.PokeBall, 73);
            PlayerInventory.Add(ItemType.DarkGem, 69);
            PlayerInventory.Add(ItemType.FluffyTail, 888);
            PlayerInventory.Add(ItemType.OvalCharm, 1);
            PlayerInventory.Add(ItemType.ShinyCharm, 1);
            ItemData.Debug_GiveAllTMHMs(PlayerInventory);
            Money = 473_123;
            InitPlayerWithDefaultLocation();
            PCBoxes = new PCBoxes();
            Daycare = new Daycare();
            Daycare.StorePokemon(PartyPokemon.CreatePlayerOwnedMon(PBESpecies.Blissey, 0, 47));
            Daycare.StorePokemon(PartyPokemon.CreatePlayerOwnedMon(PBESpecies.Ditto, 0, 82));
            PlayerParty = new Party();
            {
                // To pummel
                var pkmn1 = PartyPokemon.CreatePlayerOwnedMon(PBESpecies.Mew, 0, 67);
                pkmn1.Ability = PBEAbility.Imposter;
                //pkmn1.Item = ItemType.Leftovers;
                pkmn1.Moveset[0].Move = PBEMove.Transform;
                pkmn1.Moveset[1].Move = PBEMove.ZenHeadbutt;
                pkmn1.Moveset[2].Move = PBEMove.Surf;
                pkmn1.Moveset[3].Move = PBEMove.VCreate;
                // To pummel 2
                var pkmn2 = PartyPokemon.CreatePlayerOwnedMon(PBESpecies.Kyurem, PBEForm.Kyurem_Black, 67);
                // To pummel 3
                var pkmn3 = PartyPokemon.CreatePlayerOwnedMon(PBESpecies.Thundurus, PBEForm.Thundurus_Therian, 67);
                // To pummel 4
                var pkmn4 = PartyPokemon.CreatePlayerOwnedMon(PBESpecies.Togepi, 0, 67);
                // To pummel 5
                var pkmn5 = PartyPokemon.CreatePlayerOwnedMon(PBESpecies.Togepi, 0, 67);
                // To test evolution
                var evomon = PartyPokemon.CreatePlayerOwnedMon(PBESpecies.Chimchar, 0, 19);
                evomon.Item = ItemType.Leftovers;
                evomon.EXP = PBEDataProvider.Instance.GetEXPRequired(BaseStats.Get(evomon.Species, evomon.Form, true).GrowthRate, 20) - 1;

                GivePokemon(pkmn1);
                GivePokemon(pkmn2);
                GivePokemon(pkmn3);
                GivePokemon(pkmn4);
                GivePokemon(pkmn5);
                GivePokemon(evomon);
            }
            for (int i = 0; i < 44; i++)
            {
                Debug_GiveRandomPokemon(i == 0 || i == 1 || i == 35);
            }

            Overworld.UpdatePartyGiratinaForms(); // Not really necessary, including for debug though
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
                pkmn.Item = (ItemType)PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.GetValidItems(pkmn.Species, pkmn.Form));
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
