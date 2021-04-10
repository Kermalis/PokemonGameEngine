using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Newtonsoft.Json.Linq;
using Nuke.Common.IO;
using System;
using System.IO;

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
        }
    }
}
