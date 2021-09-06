#if DEBUG_DAYCARE_LOGEGG
using Kermalis.PokemonGameEngine.Debug;
#endif
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using System;
using System.Collections.Generic;
using System.Linq;

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

        private readonly List<DaycarePokemon> _pkmn = new(2);
        private BoxPokemon _offspring;
        private byte _offspringCounter;
        private byte _eggCycleCounter;

        public void StorePokemon(PartyPokemon pkmn)
        {
            if (_pkmn.Count >= 2)
            {
                throw new Exception();
            }

            var mon = new DaycarePokemon(pkmn);
#if DEBUG
            if (mon.Pkmn.Level < PkmnConstants.MaxLevel - 2)
            {
                mon.Debug_LevelUpManually(2);
            }
#endif
            _pkmn.Add(mon);
        }
        public string GetNickname(int index)
        {
            return _pkmn[index].Pkmn.Nickname;
        }
        public byte GetNumLevelsGained(int index)
        {
            return _pkmn[index].LevelsGained;
        }
        public void GiveEgg()
        {
            if (Engine.Instance.Save.PlayerParty.Add(new PartyPokemon(_offspring)) == -1)
            {
                throw new Exception();
            }
            _offspring = null;
        }
        public void DisposeEgg()
        {
            _offspring = null;
        }

        public DaycareState GetDaycareState()
        {
            if (_offspring is not null)
            {
                return DaycareState.EggWaiting;
            }
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
            BoxPokemon p0 = _pkmn[0].Pkmn;
            BoxPokemon p1 = _pkmn[1].Pkmn;
            return GetCompatibility(p0, p1);
        }
        public byte GetCompatibility_OvalCharm()
        {
            if (_pkmn.Count != 2)
            {
                return 0;
            }
            BoxPokemon p0 = _pkmn[0].Pkmn;
            BoxPokemon p1 = _pkmn[1].Pkmn;
            return GetCompatibility_OvalCharm(p0, p1);
        }
        private static byte GetCompatibility(BoxPokemon p0, BoxPokemon p1)
        {
            var bs0 = BaseStats.Get(p0.Species, p0.Form, true);
            var bs1 = BaseStats.Get(p1.Species, p1.Form, true);

            // Check if can't breed
            if (bs0.EggGroup1 == EggGroup.Undiscovered || bs1.EggGroup1 == EggGroup.Undiscovered)
            {
                return 0;
            }
            // Can't breed two dittos
            if (bs0.EggGroup1 == EggGroup.Ditto && bs1.EggGroup1 == EggGroup.Ditto)
            {
                return 0;
            }

            // One ditto
            if (bs0.EggGroup1 == EggGroup.Ditto || bs1.EggGroup1 == EggGroup.Ditto)
            {
                if (p0.OT.Equals(p1.OT))
                {
                    return COMPAT_LOW;
                }
                return COMPAT_MEDIUM;
            }

            // No ditto
            if (!p0.Gender.IsOppositeGender(p1.Gender))
            {
                return 0;
            }
            if (!bs0.EggGroupsOverlap(bs1))
            {
                return 0;
            }

            if (p0.Species == p1.Species)
            {
                if (p0.OT.Equals(p1.OT))
                {
                    return COMPAT_MEDIUM; // Same species, same trainer
                }
                return COMPAT_MAX; // Same species, dif trainer
            }
            if (p0.OT.Equals(p1.OT))
            {
                return COMPAT_LOW; // Dif species, same trainer
            }
            return COMPAT_MEDIUM; // Dif species, dif trainer
        }
        private static byte GetCompatibility_OvalCharm(BoxPokemon p0, BoxPokemon p1)
        {
            bool hasCharm = Engine.Instance.Save.PlayerInventory[ItemPouchType.KeyItems][ItemType.OvalCharm] is not null;
            byte compat = GetCompatibility(p0, p1);
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
        private static int GetMainSpeciesParentIndex(BoxPokemon p0, BoxPokemon p1)
        {
            // Get the female
            if (p0.Gender == PBEGender.Female)
            {
                return 0;
            }
            if (p1.Gender == PBEGender.Female)
            {
                return 1;
            }
            // No females, so get the non-Ditto parent (genderless can breed as well so don't check for a male)
            if (p0.Species == PBESpecies.Ditto)
            {
                return 1;
            }
            return 0; // p1 is Ditto
        }
        private static (PBESpecies, PBEForm) GetOffspringSpecies(BoxPokemon mainParent)
        {
            // Determine form
            PBEForm form;
            switch (mainParent.Species)
            {
                case PBESpecies.Rotom: form = PBEForm.Rotom; break; // Rotom always hatch as base form
                default: form = mainParent.Form; break; // Inherit form (Wormadam, Gastrodon, Basculin)
            }

            // Determine species
            PBESpecies earliestSpecies = new EvolutionData(mainParent.Species, mainParent.Form).BabySpecies;
            PBESpecies species = earliestSpecies;
            void ApplyIncense(ItemType incense, PBESpecies noIncenseSpecies)
            {
                if (mainParent.Item != incense)
                {
                    species = noIncenseSpecies;
                }
            }
            void ApplySplitGender(PBESpecies m, PBESpecies f)
            {
                species = PBEDataProvider.GlobalRandom.RandomBool() ? m : f;
            }
            switch (earliestSpecies)
            {
                // Incense babies
                case PBESpecies.Azurill: ApplyIncense(ItemType.SeaIncense, PBESpecies.Marill); break;
                case PBESpecies.Wynaut: ApplyIncense(ItemType.LaxIncense, PBESpecies.Wobbuffet); break;
                case PBESpecies.Budew: ApplyIncense(ItemType.RoseIncense, PBESpecies.Roselia); break;
                case PBESpecies.Chingling: ApplyIncense(ItemType.PureIncense, PBESpecies.Chimecho); break;
                case PBESpecies.Bonsly: ApplyIncense(ItemType.RockIncense, PBESpecies.Sudowoodo); break;
                case PBESpecies.MimeJr: ApplyIncense(ItemType.OddIncense, PBESpecies.MrMime); break;
                case PBESpecies.Happiny: ApplyIncense(ItemType.LuckIncense, PBESpecies.Chansey); break;
                case PBESpecies.Mantyke: ApplyIncense(ItemType.WaveIncense, PBESpecies.Mantine); break;
                case PBESpecies.Munchlax: ApplyIncense(ItemType.FullIncense, PBESpecies.Snorlax); break;
                // Split gender babies
                case PBESpecies.Nidoran_F:
                case PBESpecies.Nidoran_M: ApplySplitGender(PBESpecies.Nidoran_M, PBESpecies.Nidoran_F); break;
                case PBESpecies.Illumise:
                case PBESpecies.Volbeat: ApplySplitGender(PBESpecies.Volbeat, PBESpecies.Illumise); break;
            }
            return (species, form);
        }

        // Egg produce
        private static int GetGenderedParentIndex(BoxPokemon p0, BoxPokemon p1, PBEGender g)
        {
            if (p0.Gender == g)
            {
                return 0;
            }
            if (p1.Gender == g)
            {
                return 1;
            }
            return -1;
        }
        private static bool GetOffspringShininess(BoxPokemon p0, BoxPokemon p1)
        {
            int chance = 1;
            if (Utils.HasShinyCharm())
            {
                chance += 2;
            }
            // Masuda Method
            if (p0.OT.Language != p1.OT.Language)
            {
                chance += 5;
            }
            return PBEDataProvider.GlobalRandom.RandomBool(chance, 8192);
        }
        private static PBENature GetOffspringNature(BoxPokemon p0, BoxPokemon p1)
        {
            bool e0 = p0.Item == ItemType.Everstone;
            bool e1 = p1.Item == ItemType.Everstone;
            if (!e0 && !e1)
            {
                return PBEDataProvider.GlobalRandom.RandomElement(PBEDataUtils.AllNatures);
            }
            if (e0 && e1)
            {
                return PBEDataProvider.GlobalRandom.RandomBool() ? p0.Nature : p1.Nature;
            }
            if (e0 && !e1)
            {
                return p0.Nature;
            }
            return p1.Nature; // Only p1 has Everstone
        }
        private static (AbilityType, PBEAbility) GetOffspringAbility(BoxPokemon p0, BoxPokemon p1, BaseStats bs)
        {
            // 80% chance to pass female ability down if bred with a male
            int femaleIdx = GetGenderedParentIndex(p0, p1, PBEGender.Female);
            if (femaleIdx != -1)
            {
                int maleIdx = GetGenderedParentIndex(p0, p1, PBEGender.Male);
                if (maleIdx != -1 && PBEDataProvider.GlobalRandom.RandomBool(4, 5))
                {
                    BoxPokemon f = femaleIdx == 0 ? p0 : p1;
                    return (f.AbilType, bs.GetAbility(f.AbilType, f.Ability));
                }
            }
            AbilityType type = BaseStats.GetRandomNonHiddenAbilityType();
            PBEAbility ability = bs.GetAbility(type, PBEAbility.None);
            return (type, ability);
        }
        private static IVs GetOffspringIVs(BoxPokemon p0, BoxPokemon p1)
        {
            int RandomParent()
            {
                return PBEDataProvider.GlobalRandom.RandomInt(0, 1);
            }
            PBEStat RandomStat()
            {
                return (PBEStat)PBEDataProvider.GlobalRandom.RandomInt((int)PBEStat.HP, (int)PBEStat.Speed);
            }
            byte GetParentStat(int parent, PBEStat stat)
            {
                return (parent == 0 ? p0 : p1).IndividualValues.GetStat(stat);
            }

            byte?[] ivs = new byte?[6];
            void CopyParentIV(int parent, PBEStat stat)
            {
                ivs[(int)stat] = GetParentStat(parent, stat);
            }

            PBEStat? p0PowerItem = ItemData.GetPowerItemStat(p0.Item);
            PBEStat? p1PowerItem = ItemData.GetPowerItemStat(p1.Item);
            if (!p0PowerItem.HasValue && !p1PowerItem.HasValue)
            {
                CopyParentIV(RandomParent(), RandomStat());
            }
            else if (p0PowerItem.HasValue && !p1PowerItem.HasValue)
            {
                CopyParentIV(0, p0PowerItem.Value);
            }
            else // Parent 1 has a power item
            {
                CopyParentIV(1, p1PowerItem.Value);
            }
            // Copy two more random stats
            for (int i = 0; i < 2; i++)
            {
                PBEStat s;
                do
                {
                    s = RandomStat();
                } while (ivs[(int)s].HasValue);
                CopyParentIV(RandomParent(), s);
            }

            return new IVs(ivs);
        }
        private static BoxMoveset GetOffspringMoves(BoxPokemon p0, BoxPokemon p1, PBESpecies species, PBEForm form)
        {
            var moveset = new BoxMoveset();
            void AddMove(PBEMove move)
            {
                int empty = moveset.GetFirstEmptySlot();
                if (empty != -1)
                {
                    moveset[empty].Move = move;
                }
                else
                {
                    moveset.ShiftMovesUp();
                    moveset[PkmnConstants.NumMoves - 1].Move = move;
                }
            }
            void AddMoves(IEnumerable<PBEMove> moves)
            {
                foreach (PBEMove m in moves)
                {
                    AddMove(m);
                }
            }

            var levelUp = new LevelUpData(species, form);
            // Default moves first
            PBEMove[] defaultMoves = levelUp.GetDefaultMoves(PkmnConstants.EggHatchLevel);
            AddMoves(defaultMoves);

            // Add moves that the parents both know, that the offspring can learn by level up
            IEnumerable<PBEMove> GetNonNoneMoves(BoxPokemon bp)
            {
                return bp.Moveset.Where(ms => ms.Move != PBEMove.None).Select(ms => ms.Move);
            }
            IEnumerable<PBEMove> shared = GetNonNoneMoves(p0).Intersect(GetNonNoneMoves(p1)).Where(m => levelUp.CanLearnMoveEventually(m));
            AddMoves(shared);

            // TODO: TMHM from father or genderless parent
            int maleParent = GetGenderedParentIndex(p0, p1, PBEGender.Male);

            // Egg moves from father
            if (maleParent != -1)
            {
                IEnumerable<PBEMove> allEggMoves = EggMoves.GetEggMoves(species, form);
                IEnumerable<PBEMove> eggMoves = allEggMoves.Where(em => p0.Moveset.Contains(em) || p1.Moveset.Contains(em));
                AddMoves(eggMoves);
            }

            // Volt Tackle
            if (species == PBESpecies.Pichu && (p0.Item == ItemType.LightBall || p1.Item == ItemType.LightBall))
            {
                AddMove(PBEMove.VoltTackle);
            }

            return moveset;
        }
        private static BoxPokemon ProduceOffspring(BoxPokemon p0, BoxPokemon p1)
        {
#if DEBUG_DAYCARE_LOGEGG
            Log.WriteLine(string.Format("Parents: ({0} [{1} {2} {3}]), ({4} [{5} {6} {7}])",
                p0.Nickname, p0.Species, PBEDataUtils.GetNameOfForm(p0.Species, p0.Form), p0.Gender,
                p1.Nickname, p1.Species, PBEDataUtils.GetNameOfForm(p1.Species, p1.Form), p1.Gender));
#endif
            int mainParentIdx = GetMainSpeciesParentIndex(p0, p1);
            BoxPokemon mainParent = mainParentIdx == 0 ? p0 : p1;
#if DEBUG_DAYCARE_LOGEGG
            Log.WriteLine(string.Format("Main parent: ({0} [{1} {2} {3}])",
                mainParent.Nickname, mainParent.Species, PBEDataUtils.GetNameOfForm(mainParent.Species, mainParent.Form), mainParent.Gender));
#endif
            (PBESpecies species, PBEForm form) = GetOffspringSpecies(mainParent);
#if DEBUG_DAYCARE_LOGEGG
            Log.WriteLine(string.Format("Offspring: ({0} {1})", species, PBEDataUtils.GetNameOfForm(species, form)));
            Log.ModifyIndent(+1);
#endif
            var bs = BaseStats.Get(species, form, true);

            byte level = PkmnConstants.EggHatchLevel;
#if DEBUG_DAYCARE_LOGEGG
            Log.WriteLine("Level: " + level);
#endif
            PBEGender gender = PBEDataProvider.GlobalRandom.RandomGender(bs.GenderRatio);
#if DEBUG_DAYCARE_LOGEGG
            Log.WriteLine("Gender: " + gender);
#endif
            byte cycles = bs.EggCycles;
            uint exp = PBEDataProvider.Instance.GetEXPRequired(bs.GrowthRate, level);
            bool shiny = GetOffspringShininess(p0, p1);
#if DEBUG_DAYCARE_LOGEGG
            Log.WriteLine("Shiny: " + shiny);
#endif
            PBENature nature = GetOffspringNature(p0, p1);
#if DEBUG_DAYCARE_LOGEGG
            Log.WriteLine("Nature: " + nature);
#endif
            (AbilityType, PBEAbility) ability = GetOffspringAbility(p0, p1, bs);
#if DEBUG_DAYCARE_LOGEGG
            Log.WriteLine("Ability Type: " + ability.Item1);
            Log.WriteLine("Ability: " + ability.Item2);
#endif
            IVs ivs = GetOffspringIVs(p0, p1);
#if DEBUG_DAYCARE_LOGEGG
            Log.WriteLine("IVS: " + ivs);
#endif
            BoxMoveset moves = GetOffspringMoves(p0, p1, species, form);
#if DEBUG_DAYCARE_LOGEGG
            Log.WriteLine("Moves: " + moves);
            Log.ModifyIndent(-1);
#endif
            return BoxPokemon.CreateDaycareEgg(species, form, gender, cycles, level, exp, shiny, nature, ability, ivs, moves);
        }
        public void DoDaycareStep()
        {
            foreach (DaycarePokemon pkmn in _pkmn)
            {
                pkmn.IncrementStep();
            }

            if (_offspring is null && _pkmn.Count == 2 && ++_offspringCounter == byte.MaxValue)
            {
                BoxPokemon p0 = _pkmn[0].Pkmn;
                BoxPokemon p1 = _pkmn[1].Pkmn;
                byte compat = GetCompatibility_OvalCharm(p0, p1);
                if (compat > 0 && PBEDataProvider.GlobalRandom.RandomBool(compat, 100))
                {
#if DEBUG_DAYCARE_LOGEGG
                    Log.WriteLineWithTime("Egg produced at daycare:");
                    Log.ModifyIndent(+1);
#endif
                    _offspring = ProduceOffspring(p0, p1);
#if DEBUG_DAYCARE_LOGEGG
                    Log.ModifyIndent(-1);
#endif
                }
            }
        }

        // Egg hatch
        private static byte GetEggCyclesToSubtract()
        {
            foreach (PartyPokemon p in Engine.Instance.Save.PlayerParty)
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
            foreach (PartyPokemon p in Engine.Instance.Save.PlayerParty)
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
