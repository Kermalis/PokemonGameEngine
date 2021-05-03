using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class Daycare
    {
        // In percentages
        private const byte COMPAT_LOW = 20;
        private const byte COMPAT_MEDIUM = 50;
        private const byte COMPAT_MAX = 70;
        private const byte COMPAT_OVALCHARM_LOW = 40;
        private const byte COMPAT_OVALCHARM_MEDIUM = 80;
        private const byte COMPAT_OVALCHARM_MAX = 88;

        private readonly List<DaycarePokemon> _pkmn = new List<DaycarePokemon>(2);
        private byte _eggCycleCounter;

        public void IncrementStep()
        {
            foreach (DaycarePokemon pkmn in _pkmn)
            {
                pkmn.IncrementStep();
            }
        }

        public void StorePokemon(PartyPokemon pkmn)
        {
            if (_pkmn.Count >= 2)
            {
                throw new Exception();
            }

            _pkmn.Add(new DaycarePokemon(pkmn));
        }

        public DaycareState GetDaycareState()
        {
            // TODO: Check for egg
            switch (_pkmn.Count)
            {
                case 0: return DaycareState.NoPokemon;
                case 1: return DaycareState.OnePokemon;
                case 2: return DaycareState.TwoPokemon;
            }
            throw new Exception();
        }

        /// <summary>Returns a value between 0 and 100.</summary>
        public byte GetCompatibility()
        {
            if (_pkmn.Count != 2)
            {
                return 0;
            }

            BoxPokemon pkmn1 = _pkmn[0].Pkmn;
            BoxPokemon pkmn2 = _pkmn[1].Pkmn;
            var bs1 = new BaseStats(pkmn1.Species, pkmn1.Form);
            var bs2 = new BaseStats(pkmn1.Species, pkmn1.Form);

            // Check if can't breed
            if (bs1.EggGroup1 == EggGroup.Undiscovered || bs2.EggGroup1 == EggGroup.Undiscovered)
            {
                return 0;
            }
            // Can't breed two dittos
            if (bs1.EggGroup1 == EggGroup.Ditto && bs2.EggGroup1 == EggGroup.Ditto)
            {
                return 0;
            }

            // One ditto
            if (bs1.EggGroup1 == EggGroup.Ditto || bs2.EggGroup1 == EggGroup.Ditto)
            {
                if (pkmn1.OT.Equals(pkmn2.OT))
                {
                    return COMPAT_LOW;
                }
                return COMPAT_MEDIUM;
            }

            // No ditto
            if (!pkmn1.Gender.IsOppositeGender(pkmn2.Gender))
            {
                return 0;
            }
            if (!bs1.EggGroupsOverlap(bs2))
            {
                return 0;
            }

            if (pkmn1.Species == pkmn2.Species)
            {
                if (pkmn1.OT.Equals(pkmn2.OT))
                {
                    return COMPAT_MEDIUM; // Same species, same trainer
                }
                return COMPAT_MAX; // Same species, dif trainer
            }
            if (pkmn1.OT.Equals(pkmn2.OT))
            {
                return COMPAT_LOW; // Dif species, same trainer
            }
            return COMPAT_MEDIUM; // Dif species, dif trainer
        }
        public byte GetCompatibility_OvalCharm()
        {
            bool hasCharm = Game.Instance.Save.PlayerInventory[ItemPouchType.KeyItems][(PBEItem)631] != null; // 631 is Oval Charm
            byte compat = GetCompatibility();
            if (hasCharm)
            {
                switch (compat)
                {
                    case COMPAT_LOW: return COMPAT_OVALCHARM_LOW;
                    case COMPAT_MEDIUM: return COMPAT_OVALCHARM_MEDIUM;
                    case COMPAT_MAX: return COMPAT_OVALCHARM_MAX;
                }
            }
            return compat;
        }

        // Egg hatch
        private byte GetEggCyclesToSubtract()
        {
            foreach (PartyPokemon p in Game.Instance.Save.PlayerParty)
            {
                if (p.IsEgg)
                {
                    continue;
                }
                PBEAbility a = p.Ability;
                if (a == PBEAbility.FlameBody || a == PBEAbility.MagmaArmor)
                {
                    return 2;
                }
            }
            return 1;
        }
        public void DoEggCycleStep()
        {
            if (_eggCycleCounter < byte.MaxValue)
            {
                _eggCycleCounter++;
                return;
            }

            _eggCycleCounter = 0;
            byte toRemove = GetEggCyclesToSubtract();
            foreach (PartyPokemon p in Game.Instance.Save.PlayerParty)
            {
                if (p.IsEgg && p.Friendship > 0)
                {
                    int result = p.Friendship - toRemove;
                    p.Friendship = result < 0 ? (byte)0 : (byte)result;
                }
            }
        }
    }
}
