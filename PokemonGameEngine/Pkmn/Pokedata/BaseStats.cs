using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Util;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Pkmn.Pokedata
{
    internal sealed class BaseStats : IPBEPokemonData
    {
        public PBESpecies Species { get; }
        public PBEForm Form { get; }

        public PBEReadOnlyStatCollection Stats { get; }
        public PBEType Type1 { get; }
        public PBEType Type2 { get; }
        public byte CatchRate { get; }
        public byte BaseFriendship { get; }
        public PBEGenderRatio GenderRatio { get; }
        public PBEGrowthRate GrowthRate { get; }
        public ushort BaseEXPYield { get; }
        public EggGroup EggGroup1 { get; }
        public EggGroup EggGroup2 { get; }
        public PBEAbility Ability1 { get; }
        public PBEAbility Ability2 { get; }
        public PBEAbility AbilityH { get; }
        public byte FleeRate { get; }
        public double Weight { get; }

        IPBEReadOnlyStatCollection IPBEPokemonData.BaseStats => Stats;
        private readonly List<PBEAbility> _abilities;
        public IReadOnlyList<PBEAbility> Abilities => _abilities;

        public BaseStats(PBESpecies species, PBEForm form)
        {
            Species = species;
            Form = form;

            string resource = "Pokedata." + PkmnOrderResolver.GetDirectoryName(species, form) + ".BaseStats.bin";
            using (var r = new EndianBinaryReader(Utils.GetResourceStream(resource)))
            {
                Stats = new PBEReadOnlyStatCollection(r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte());
                Type1 = r.ReadEnum<PBEType>();
                Type2 = r.ReadEnum<PBEType>();
                CatchRate = r.ReadByte();
                BaseFriendship = r.ReadByte();
                GenderRatio = r.ReadEnum<PBEGenderRatio>();
                GrowthRate = r.ReadEnum<PBEGrowthRate>();
                BaseEXPYield = r.ReadUInt16();
                EggGroup1 = r.ReadEnum<EggGroup>();
                EggGroup2 = r.ReadEnum<EggGroup>();
                Ability1 = r.ReadEnum<PBEAbility>();
                Ability2 = r.ReadEnum<PBEAbility>();
                AbilityH = r.ReadEnum<PBEAbility>();
                FleeRate = r.ReadByte();
                Weight = r.ReadDouble();
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
    }
}
