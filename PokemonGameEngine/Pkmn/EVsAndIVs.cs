using Kermalis.PokemonBattleEngine.Data;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class EVs : IPBEStatCollection
    {
        public byte HP { get; set; }
        public byte Attack { get; set; }
        public byte Defense { get; set; }
        public byte SpAttack { get; set; }
        public byte SpDefense { get; set; }
        public byte Speed { get; set; }

        public void CopyFrom(IPBEReadOnlyStatCollection other)
        {
            HP = other.HP;
            Attack = other.Attack;
            Defense = other.Defense;
            SpAttack = other.SpAttack;
            SpDefense = other.SpDefense;
            Speed = other.Speed;
        }
    }

    internal sealed class IVs : IPBEReadOnlyStatCollection
    {
        public byte HP { get; }
        public byte Attack { get; }
        public byte Defense { get; }
        public byte SpAttack { get; }
        public byte SpDefense { get; }
        public byte Speed { get; }

        public IVs(byte?[] ivs)
            : this(ivs[0], ivs[1], ivs[2], ivs[3], ivs[4], ivs[5]) { }
        public IVs(byte? hp = null, byte? attack = null, byte? defense = null, byte? spAttack = null, byte? spDefense = null, byte? speed = null)
        {
            HP = hp ?? (byte)PBEDataProvider.GlobalRandom.RandomInt(0, 31);
            Attack = attack ?? (byte)PBEDataProvider.GlobalRandom.RandomInt(0, 31);
            Defense = defense ?? (byte)PBEDataProvider.GlobalRandom.RandomInt(0, 31);
            SpAttack = spAttack ?? (byte)PBEDataProvider.GlobalRandom.RandomInt(0, 31);
            SpDefense = spDefense ?? (byte)PBEDataProvider.GlobalRandom.RandomInt(0, 31);
            Speed = speed ?? (byte)PBEDataProvider.GlobalRandom.RandomInt(0, 31);
        }
        public IVs(byte hp, byte attack, byte defense, byte spAttack, byte spDefense, byte speed)
        {
            HP = hp;
            Attack = attack;
            Defense = defense;
            SpAttack = spAttack;
            SpDefense = spDefense;
            Speed = speed;
        }
        public IVs(IPBEReadOnlyStatCollection other)
        {
            HP = other.HP;
            Attack = other.Attack;
            Defense = other.Defense;
            SpAttack = other.SpAttack;
            SpDefense = other.SpDefense;
            Speed = other.Speed;
        }
    }
}
