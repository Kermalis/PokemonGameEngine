using Kermalis.PokemonBattleEngine.Data;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class Pokerus
    {
        private byte _b;
        public byte Strain
        {
            get => (byte)(_b >> 4);
            set => _b = (byte)((_b & 0xF) | (value << 4));
        }
        public byte DaysRemaining
        {
            get => (byte)(_b & 0xF);
            set => _b = (byte)((_b & ~0xF) | value);
        }
        public bool Exists => Strain != 0;
        public bool IsCured => Strain != 0 && DaysRemaining == 0;
        public bool IsInfected => Strain != 0 && DaysRemaining > 0;

        // Create
        public Pokerus(bool empty)
        {
            if (!empty)
            {
                CreateRandomStrain();
            }
        }
        // Clone
        public Pokerus(Pokerus other)
        {
            _b = other._b;
        }

        private void CreateRandomStrain()
        {
            Strain = (byte)PBEDataProvider.GlobalRandom.RandomInt(1, 15);
            DaysRemaining = GetInitialDaysRemaining(Strain);
        }
        private void SpreadStrain(byte strain)
        {
            Strain = strain;
            DaysRemaining = GetInitialDaysRemaining(strain);
        }

        public static byte GetInitialDaysRemaining(byte strain)
        {
            return (byte)((strain % 4) + 1);
        }

        public static void TryCreatePokerus(Party party)
        {
            if (!PBEDataProvider.GlobalRandom.RandomBool(3, 0x10000))
            {
                return;
            }

            while (true)
            {
                PartyPokemon pkmn = party[PBEDataProvider.GlobalRandom.RandomInt(0, party.Count - 1)];
                if (pkmn.Pokerus.Exists)
                {
                    return; // Nothing happens if we chose one who was already infected
                }
                if (!pkmn.IsEgg)
                {
                    pkmn.Pokerus.CreateRandomStrain();
                    return;
                }
            }
        }
        public static void TrySpreadPokerus(Party party)
        {
            if (!PBEDataProvider.GlobalRandom.RandomBool(1, 3))
            {
                return;
            }

            for (int i = 0; i < party.Count; i++)
            {
                PartyPokemon p = party[i];
                Pokerus pkrs = p.Pokerus;
                if (!pkrs.Exists || pkrs.DaysRemaining == 0)
                {
                    continue;
                }
                // Spread to members on the left and right only
                if (i != 0)
                {
                    PartyPokemon p2 = party[i - 1];
                    Pokerus pkrs2 = p2.Pokerus;
                    if (!pkrs2.Exists)
                    {
                        pkrs2.SpreadStrain(pkrs.Strain);
                    }
                }
                if (i < party.Count - 1)
                {
                    PartyPokemon p2 = party[i + 1];
                    Pokerus pkrs2 = p2.Pokerus;
                    if (!pkrs2.Exists)
                    {
                        pkrs2.SpreadStrain(pkrs.Strain);
                        i++; // Skip this one on the right because we wouldn't want it to spread right after receiving it
                    }
                }
            }
        }
    }
}
