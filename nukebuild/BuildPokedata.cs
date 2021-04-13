using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Newtonsoft.Json.Linq;
using Nuke.Common.IO;
using System;
using System.IO;

internal static class PokedataBuilderHelper
{
    public static PBEForm FormValue(this JToken j)
    {
        string strForm = j.Value<string>();
        if (strForm == null)
        {
            return 0;
        }
        else
        {
            return (PBEForm)Enum.Parse(typeof(PBEForm), strForm);
        }
    }
}

public sealed partial class Build
{
    private sealed class BaseStats
    {
        private readonly byte HP;
        private readonly byte Attack;
        private readonly byte Defense;
        private readonly byte SpAttack;
        private readonly byte SpDefense;
        private readonly byte Speed;
        private readonly PBEType Type1;
        private readonly PBEType Type2;
        private readonly byte CatchRate;
        private readonly byte BaseFriendship;
        private readonly PBEGenderRatio GenderRatio;
        private readonly EggGroup EggGroup1;
        private readonly EggGroup EggGroup2;
        private readonly PBEAbility Ability1;
        private readonly PBEAbility Ability2;
        private readonly PBEAbility AbilityH;
        private readonly byte FleeRate;
        private readonly double Weight;

        public BaseStats(JToken j)
        {
            HP = j[nameof(HP)].Value<byte>();
            Attack = j[nameof(Attack)].Value<byte>();
            Defense = j[nameof(Defense)].Value<byte>();
            SpAttack = j[nameof(SpAttack)].Value<byte>();
            SpDefense = j[nameof(SpDefense)].Value<byte>();
            Speed = j[nameof(Speed)].Value<byte>();
            Type1 = j[nameof(Type1)].EnumValue<PBEType>();
            Type2 = j[nameof(Type2)].EnumValue<PBEType>();
            CatchRate = j[nameof(CatchRate)].Value<byte>();
            BaseFriendship = j[nameof(BaseFriendship)].Value<byte>();
            GenderRatio = j[nameof(GenderRatio)].EnumValue<PBEGenderRatio>();
            EggGroup1 = j[nameof(EggGroup1)].EnumValue<EggGroup>();
            EggGroup2 = j[nameof(EggGroup2)].EnumValue<EggGroup>();
            Ability1 = j[nameof(Ability1)].EnumValue<PBEAbility>();
            Ability2 = j[nameof(Ability2)].EnumValue<PBEAbility>();
            AbilityH = j[nameof(AbilityH)].EnumValue<PBEAbility>();
            FleeRate = j[nameof(FleeRate)].Value<byte>();
            Weight = j[nameof(Weight)].Value<double>();
        }

        public void Write(EndianBinaryWriter w)
        {
            w.Write(HP);
            w.Write(Attack);
            w.Write(Defense);
            w.Write(SpAttack);
            w.Write(SpDefense);
            w.Write(Speed);
            w.Write(Type1);
            w.Write(Type2);
            w.Write(CatchRate);
            w.Write(BaseFriendship);
            w.Write(GenderRatio);
            w.Write(EggGroup1);
            w.Write(EggGroup2);
            w.Write(Ability1);
            w.Write(Ability2);
            w.Write(AbilityH);
            w.Write(FleeRate);
            w.Write(Weight);
        }
    }
    private sealed class Evos
    {
        private sealed class Evo
        {
            private readonly EvoMethod Method;
            private readonly ushort Param;
            private readonly PBESpecies Species;
            private readonly PBEForm Form;

            public Evo(JToken j)
            {
                Method = j[nameof(Method)].EnumValue<EvoMethod>();
                switch (Method)
                {
                    case EvoMethod.Friendship_LevelUp:
                    case EvoMethod.Friendship_Day_LevelUp:
                    case EvoMethod.Friendship_Night_LevelUp:
                    {
                        Param = j["FriendshipRequired"].Value<ushort>();
                        break;
                    }
                    case EvoMethod.LevelUp:
                    case EvoMethod.ATK_GT_DEF_LevelUp:
                    case EvoMethod.ATK_EE_DEF_LevelUp:
                    case EvoMethod.ATK_LT_DEF_LevelUp:
                    case EvoMethod.Silcoon_LevelUp:
                    case EvoMethod.Cascoon_LevelUp:
                    case EvoMethod.Ninjask_LevelUp:
                    case EvoMethod.Shedinja_LevelUp:
                    case EvoMethod.Male_LevelUp:
                    case EvoMethod.Female_LevelUp:
                    {
                        Param = j["LevelRequired"].Value<ushort>();
                        break;
                    }
                    case EvoMethod.Beauty_LevelUp:
                    {
                        Param = j["BeautyRequired"].Value<ushort>();
                        break;
                    }
                    case EvoMethod.Item_Trade:
                    case EvoMethod.Stone:
                    case EvoMethod.Male_Stone:
                    case EvoMethod.Female_Stone:
                    case EvoMethod.Item_Day_LevelUp:
                    case EvoMethod.Item_Night_LevelUp:
                    {
                        Param = (ushort)j["ItemRequired"].EnumValue<PBEItem>();
                        break;
                    }
                    case EvoMethod.Move_LevelUp:
                    {
                        Param = (ushort)j["MoveRequired"].EnumValue<PBEMove>();
                        break;
                    }
                    case EvoMethod.PartySpecies_LevelUp:
                    {
                        Param = (ushort)j["SpeciesRequired"].EnumValue<PBESpecies>();
                        break;
                    }
                }
                Species = j[nameof(Species)].EnumValue<PBESpecies>();
                Form = j[nameof(Form)].FormValue();
            }

            public void Write(EndianBinaryWriter w)
            {
                w.Write(Method);
                w.Write(Param);
                w.Write(Species);
                w.Write(Form);
            }
        }
        private readonly PBESpecies BabySpecies;
        private readonly Evo[] Evolutions;

        public Evos(JToken j)
        {
            BabySpecies = j[nameof(BabySpecies)].EnumValue<PBESpecies>();
            var evos = (JArray)j[nameof(Evolutions)];
            int numEvos = evos.Count;
            Evolutions = new Evo[numEvos];
            for (int i = 0; i < numEvos; i++)
            {
                Evolutions[i] = new Evo(evos[i]);
            }
        }

        public void Write(EndianBinaryWriter w)
        {
            w.Write(BabySpecies);
            byte count = (byte)Evolutions.Length;
            w.Write(count);
            for (int i = 0; i < count; i++)
            {
                Evolutions[i].Write(w);
            }
        }
    }
    private sealed class LevelUps
    {
        private sealed class LevelUp
        {
            private readonly PBEMove Move;
            private readonly byte Level;

            public LevelUp(JToken j)
            {
                Move = j[nameof(Move)].EnumValue<PBEMove>();
                Level = j[nameof(Level)].Value<byte>();
            }

            public void Write(EndianBinaryWriter w)
            {
                w.Write(Move);
                w.Write(Level);
            }
        }
        private readonly LevelUp[] LevelUpMoves;

        public LevelUps(JToken j)
        {
            var moves = (JArray)j[nameof(LevelUpMoves)];
            int numMoves = moves.Count;
            LevelUpMoves = new LevelUp[numMoves];
            for (int i = 0; i < numMoves; i++)
            {
                LevelUpMoves[i] = new LevelUp(moves[i]);
            }
        }

        public void Write(EndianBinaryWriter w)
        {
            byte count = (byte)LevelUpMoves.Length;
            w.Write(count);
            for (int i = 0; i < count; i++)
            {
                LevelUpMoves[i].Write(w);
            }
        }
    }
    private sealed class EggMovs
    {
        private readonly PBEMove[] EggMoves;

        public EggMovs(JToken j)
        {
            var moves = (JArray)j[nameof(EggMoves)];
            int numMoves = moves.Count;
            EggMoves = new PBEMove[numMoves];
            for (int i = 0; i < numMoves; i++)
            {
                EggMoves[i] = moves[i]["Move"].EnumValue<PBEMove>();
            }
        }

        public void Write(EndianBinaryWriter w)
        {
            byte count = (byte)EggMoves.Length;
            w.Write(count);
            w.Write(EggMoves, 0, count);
        }
    }

    private static readonly AbsolutePath PokedataPath = AssetPath / "Pokedata";

    private void CleanPokedata()
    {
        foreach (string dir in Directory.GetDirectories(PokedataPath))
        {
            foreach (string file in ((AbsolutePath)dir).GlobFiles("*.bin"))
            {
                File.Delete(file);
            }
        }
    }
    private void BuildPokedata()
    {
        foreach (string dir in Directory.GetDirectories(PokedataPath))
        {
            string dirName = Path.GetFileName(dir);
            PBEForm form;
            if (Enum.TryParse(dirName, out PBESpecies species))
            {
                form = 0;
            }
            else
            {
                form = Enum.Parse<PBEForm>(dirName);
                string[] split = dirName.Split('_');
                if (split.Length != 2)
                {
                    throw new Exception();
                }
                species = Enum.Parse<PBESpecies>(split[0]);
            }

            #region Base Stats
            {
                var json = JObject.Parse(File.ReadAllText(Path.Combine(dir, "BaseStats.json")));
                using (var w = new EndianBinaryWriter(File.Create(Path.Combine(dir, "BaseStats.bin"))))
                {
                    new BaseStats(json).Write(w);
                }
            }
            #endregion

            #region Evolutions
            {
                var json = JObject.Parse(File.ReadAllText(Path.Combine(dir, "Evolutions.json")));
                using (var w = new EndianBinaryWriter(File.Create(Path.Combine(dir, "Evolutions.bin"))))
                {
                    new Evos(json).Write(w);
                }
            }
            #endregion

            #region Level Up
            {
                var json = JObject.Parse(File.ReadAllText(Path.Combine(dir, "LevelUp.json")));
                using (var w = new EndianBinaryWriter(File.Create(Path.Combine(dir, "LevelUp.bin"))))
                {
                    new LevelUps(json).Write(w);
                }
            }
            #endregion

            #region Egg Moves
            {
                var json = JObject.Parse(File.ReadAllText(Path.Combine(dir, "EggMoves.json")));
                using (var w = new EndianBinaryWriter(File.Create(Path.Combine(dir, "EggMoves.bin"))))
                {
                    new EggMovs(json).Write(w);
                }
            }
            #endregion
        }
    }
}
