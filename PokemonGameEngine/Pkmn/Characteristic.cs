using Kermalis.PokemonBattleEngine.Data;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal static class Characteristic
    {
        private const int NumCharacteristics = 5;

        #region Tables

        private static readonly string[] _hpStrs = new string[NumCharacteristics]
        {
            "Loves to eat",
            "Takes plenty of siestas",
            "Nods off a lot",
            "Scatters things often",
            "Likes to relax"
        };
        private static readonly string[] _attackStrs = new string[NumCharacteristics]
        {
            "Proud of its power",
            "Likes to thrash about",
            "A little quick tempered",
            "Likes to fight",
            "Quick tempered"
        };
        private static readonly string[] _defenseStrs = new string[NumCharacteristics]
        {
            "Proud of its power",
            "Likes to thrash about",
            "A little quick tempered",
            "Likes to fight",
            "Quick tempered"
        };
        private static readonly string[] _spAttackStrs = new string[NumCharacteristics]
        {
            "Highly curious",
            "Mischievous",
            "Thoroughly cunning",
            "Often lost in thought",
            "Very finicky"
        };
        private static readonly string[] _spDefenseStrs = new string[NumCharacteristics]
        {
            "Strong willed",
            "Somewhat vain",
            "Strongly defiant",
            "Hates to lose",
            "Somewhat stubborn"
        };
        private static readonly string[] _speedStrs = new string[NumCharacteristics]
        {
            "Likes to run",
            "Alert to sounds",
            "Impetuous and silly",
            "Somewhat of a clown",
            "Quick to flee"
        };
        private static readonly Dictionary<PBEStat, string[]> _dict = new(6)
        {
            { PBEStat.HP, _hpStrs },
            { PBEStat.Attack, _attackStrs },
            { PBEStat.Defense, _defenseStrs },
            { PBEStat.SpAttack, _spAttackStrs },
            { PBEStat.SpDefense, _spDefenseStrs },
            { PBEStat.Speed, _speedStrs }
        };

        #endregion

        public static string GetCharacteristic(PBEStat stat, byte iv)
        {
            return _dict[stat][iv % NumCharacteristics];
        }
        public static string GetCharacteristic(uint pid, IVs ivs)
        {
            var tied = new List<PBEStat>(6);
            byte highest = 0;
            void Check(PBEStat stat, byte val)
            {
                if (val < highest)
                {
                    return;
                }
                if (val > highest)
                {
                    highest = val;
                    tied.Clear();
                }
                tied.Add(stat);
            }

            Check(PBEStat.HP, ivs.HP);
            Check(PBEStat.Attack, ivs.Attack);
            Check(PBEStat.Defense, ivs.Defense);
            Check(PBEStat.SpAttack, ivs.SpAttack);
            Check(PBEStat.SpDefense, ivs.SpDefense);
            Check(PBEStat.Speed, ivs.Speed);

            if (tied.Count == 1)
            {
                return GetCharacteristic(tied[0], highest);
            }

            var tieStatOrder = new PBEStat[6] { PBEStat.HP, PBEStat.Attack, PBEStat.Defense, PBEStat.Speed, PBEStat.SpAttack, PBEStat.SpDefense };
            uint index = pid % 6;
            while (true)
            {
                PBEStat stat = tieStatOrder[index];
                if (tied.Contains(stat))
                {
                    return GetCharacteristic(stat, highest);
                }
                if (++index >= 6)
                {
                    index = 0;
                }
            }
        }
    }
}
