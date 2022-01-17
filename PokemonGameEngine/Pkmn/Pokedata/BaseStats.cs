using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using System;
using System.Collections.Generic;
using System.IO;
#if DEBUG_DATA_CACHE
using Kermalis.PokemonBattleEngine.Data.Utils;
#endif

namespace Kermalis.PokemonGameEngine.Pkmn.Pokedata
{
    internal sealed class BaseStats : IPBEPokemonData
    {
        private const int CACHE_LENGTH = 20;

        public PBESpecies Species { get; }
        public PBEForm Form { get; }

        public PBEReadOnlyStatCollection Stats { get; }
        public PBEType Type1 { get; }
        public PBEType Type2 { get; }
        public byte CatchRate { get; }
        public byte BaseFriendship { get; }
        public byte EggCycles { get; }
        public PBEGenderRatio GenderRatio { get; }
        public PBEGrowthRate GrowthRate { get; }
        public ushort BaseEXPYield { get; }
        public EggGroup EggGroup1 { get; }
        public EggGroup EggGroup2 { get; }
        public PBEAbility Ability1 { get; }
        public PBEAbility Ability2 { get; }
        public PBEAbility AbilityH { get; }
        public byte FleeRate { get; }
        public float Weight { get; }

        IPBEReadOnlyStatCollection IPBEPokemonData.BaseStats => Stats;
        private readonly List<PBEAbility> _abilities;
        public IReadOnlyList<PBEAbility> Abilities => _abilities;

        private static readonly List<BaseStats> _cache = new(CACHE_LENGTH);

        public static BaseStats Get(PBESpecies species, PBEForm form, bool cache)
        {
            int i;
            BaseStats bs;
            // Try to find in the cache first
            for (i = 0; i < _cache.Count; i++)
            {
                bs = _cache[i];
                if (bs.Species == species && bs.Form == form)
                {
                    if (i != _cache.Count - 1)
                    {
                        _cache.RemoveAt(i); // Remove from its position and put it at the end
                        _cache.Add(bs);
#if DEBUG_DATA_CACHE
                        Console.WriteLine("BaseStats cache - Moving {0}_{1} (from {2} to {3})", species, PBEDataUtils.GetNameOfForm(species, form), i, _cache.Count - 1);
#endif
                    }
                    return bs;
                }
            }
            // Did not find in the cache, so create it
            bs = new BaseStats(species, form);
            if (!cache)
            {
                return bs;
            }
            if (i < CACHE_LENGTH - 1)
            {
                _cache.Add(bs); // Still room in the cache to add this at the end
#if DEBUG_DATA_CACHE
                Console.WriteLine("BaseStats cache - Adding {0}_{1} ({2})", species, PBEDataUtils.GetNameOfForm(species, form), i);
#endif
            }
            else
            {
#if DEBUG_DATA_CACHE
                BaseStats old = _cache[0];
                PBESpecies oldSpecies = old.Species;
                PBEForm oldForm = old.Form;
#endif
                _cache.RemoveAt(0); // Remove oldest and add to end
                _cache.Add(bs);
#if DEBUG_DATA_CACHE
                Console.WriteLine("BaseStats cache - Removing {0}_{1}", oldSpecies, PBEDataUtils.GetNameOfForm(oldSpecies, oldForm));
                Console.WriteLine("BaseStats cache - Adding {0}_{1} ({2})", species, PBEDataUtils.GetNameOfForm(species, form), _cache.Count - 1);
#endif
            }
            return bs;
        }

        private BaseStats(PBESpecies species, PBEForm form)
        {
            Species = species;
            Form = form;

            string asset = @"Pokedata\" + AssetLoader.GetPkmnDirectoryName(species, form) + @"\BaseStats.bin";
            using (var r = new EndianBinaryReader(File.OpenRead(AssetLoader.GetPath(asset))))
            {
                Stats = new PBEReadOnlyStatCollection(r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte());
                Type1 = r.ReadEnum<PBEType>();
                Type2 = r.ReadEnum<PBEType>();
                CatchRate = r.ReadByte();
                BaseFriendship = r.ReadByte();
                EggCycles = r.ReadByte();
                GenderRatio = r.ReadEnum<PBEGenderRatio>();
                GrowthRate = r.ReadEnum<PBEGrowthRate>();
                BaseEXPYield = r.ReadUInt16();
                EggGroup1 = r.ReadEnum<EggGroup>();
                EggGroup2 = r.ReadEnum<EggGroup>();
                Ability1 = r.ReadEnum<PBEAbility>();
                Ability2 = r.ReadEnum<PBEAbility>();
                AbilityH = r.ReadEnum<PBEAbility>();
                FleeRate = r.ReadByte();
                Weight = r.ReadSingle();
            }

            _abilities = new List<PBEAbility>(3);
            _abilities.Add(Ability1);
            if (Ability2 != PBEAbility.None && !_abilities.Contains(Ability2))
            {
                _abilities.Add(Ability2);
            }
            if (AbilityH != PBEAbility.None && !_abilities.Contains(AbilityH))
            {
                _abilities.Add(AbilityH);
            }
        }

        public bool EggGroupsOverlap(BaseStats other)
        {
            return EggGroup1 == other.EggGroup1 || EggGroup1 == other.EggGroup2
                || EggGroup2 == other.EggGroup1 || EggGroup2 == other.EggGroup2;
        }
        public static AbilityType GetRandomNonHiddenAbilityType()
        {
            return PBEDataProvider.GlobalRandom.RandomBool() ? AbilityType.Ability1 : AbilityType.Ability2;
        }
        public PBEAbility GetAbility(AbilityType type, PBEAbility cur)
        {
            switch (type)
            {
                case AbilityType.Ability1: return Ability1;
                case AbilityType.Ability2: return Ability2 == PBEAbility.None ? Ability1 : Ability2;
                case AbilityType.AbilityH: return AbilityH;
                case AbilityType.NonStandard: return cur;
            }
            throw new Exception();
        }
    }
}
