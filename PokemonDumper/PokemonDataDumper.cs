using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.SimpleNARC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonDumper
{
    internal static partial class PokemonDataDumper
    {
        private static Dictionary<(PBESpecies, PBEForm), Pokemon> _dict;

        // You must dump everything yourself
        // Dumps probably work across all regions, not sure
        //
        // B2 and W2 evolution NARC is /a/0/1/9 (B2 and W2 have identical evolution NARCs)
        // B, W, B2, and W2 level-up move NARC is /a/0/1/8 (B and W have identical level-up move NARCs) (B2 and W2 have identical level-up move NARCs)
        // B, W, B2, and W2 TMHM moves are in the Pokémon data NARC which is /a/0/1/6 (B and W have identical Pokémon data NARCs) (B2 and W2 have identical Pokémon data NARCs)
        // B2 and W2 tutor compatibility is in the Pokémon data NARC which is /a/0/1/6 (B2 and W2 have identical Pokémon data NARCs)
        // B and W egg move NARC is /a/1/2/3, B2 and W2 egg move NARC is /a/1/2/4 (B, W, B2, and W2 have identical egg move NARCs)
        public static void Main()
        {
            // Read all data
            {
                var b2w2Pokedata = new NARC(@"../../../\DumpedData\B2W2Pokedata.narc");
                var b2w2Evolution = new NARC(@"../../../\DumpedData\B2W2Evolution.narc");
                var b2w2LevelUp = new NARC(@"../../../\DumpedData\B2W2LevelUp.narc");
                var bwb2w2Egg = new NARC(@"../../../\DumpedData\BWB2W2Egg.narc");

                _dict = new Dictionary<(PBESpecies, PBEForm), Pokemon>();

                ReadPokemonData(b2w2Pokedata);
                ReadEvolutions(b2w2Evolution);
                ReadLevelUpMoves(b2w2LevelUp);
                ReadTMHMMoves(b2w2Pokedata);
                ReadMoveTutorMoves(b2w2Pokedata);
                ReadEggMoves(bwb2w2Egg);
            }

            FixForms();
            FixArceus();
            GiveFormsEggMoves();

            foreach (KeyValuePair<(PBESpecies, PBEForm), Pokemon> tup in _dict)
            {
                (PBESpecies species, PBEForm form) = tup.Key;
                string dir = GetDirectory(species, form);
                Pokemon pkmn = tup.Value;
                WritePokemonData(dir, pkmn);
                WriteEvolutions(dir, pkmn);
                WriteLevelUpMoves(dir, pkmn);
                WriteEggMoves(dir, pkmn);
            }
        }

        private static string GetDirectory(PBESpecies species, PBEForm form)
        {
            string dir;
            if (form == 0)
            {
                dir = species.ToString();
            }
            else
            {
                dir = PBEDataUtils.GetNameOfForm(species, form);
            }
            dir = Path.Combine(@"../../../../PokemonGameEngine/Assets/Pokedata", dir);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        private static Pokemon AddSpecies((PBESpecies, PBEForm) key)
        {
            if (!_dict.TryGetValue(key, out Pokemon pkmn))
            {
                pkmn = new Pokemon();
                _dict.Add(key, pkmn);
            }
            return pkmn;
        }
        private static void AddOtherMove((PBESpecies, PBEForm) key, PBEMove move, PBEMoveObtainMethod flag)
        {
            Pokemon pkmn = AddSpecies(key);
            Dictionary<PBEMove, PBEMoveObtainMethod> other = pkmn.OtherMoves;
            if (other.ContainsKey(move))
            {
                other[move] |= flag;
            }
            else
            {
                other.Add(move, flag);
            }
        }

        private static void ReadPokemonData(NARC b2w2Pokedata)
        {
            for (int sp = 1; sp <= 708; sp++)
            {
                // Skip Egg, Bad Egg, and Pokéstar Studios Pokémon
                if (sp > 649 && sp < 685)
                {
                    continue;
                }

                if (!_b2w2SpeciesIndexToPBESpecies.TryGetValue(sp, out (PBESpecies, PBEForm) key))
                {
                    key = ((PBESpecies)sp, 0);
                }
                using (var pokedata = new EndianBinaryReader(new MemoryStream(b2w2Pokedata[sp]), Endianness.LittleEndian))
                {
                    Pokemon pkmn = AddSpecies(key);
                    pkmn.HP = pokedata.ReadByte(0x0);
                    pkmn.Attack = pokedata.ReadByte(0x1);
                    pkmn.Defense = pokedata.ReadByte(0x2);
                    pkmn.Speed = pokedata.ReadByte(0x3);
                    pkmn.SpAttack = pokedata.ReadByte(0x4);
                    pkmn.SpDefense = pokedata.ReadByte(0x5);
                    pkmn.Type1 = _gen5Types[pokedata.ReadByte(0x6)];
                    pkmn.Type2 = _gen5Types[pokedata.ReadByte(0x7)];
                    if (pkmn.Type1 == pkmn.Type2)
                    {
                        pkmn.Type2 = PBEType.None;
                    }
                    pkmn.CatchRate = pokedata.ReadByte(0x8);
                    // 0x9 - stage (0 = missingno, 1 baby, 2 notfullyevolved, 3 fullyevolved)
                    // 0xA-0xB - evs
                    // 0xC-0xD - Item1
                    // 0xE-0xF - Item2
                    // 0x10-0x11 - Item3
                    pkmn.GenderRatio = (PBEGenderRatio)pokedata.ReadByte(0x12);
                    // 0x13 - hatch cycle
                    pkmn.BaseFriendship = pokedata.ReadByte(0x14);
                    pkmn.GrowthRate = (PBEGrowthRate)pokedata.ReadByte(0x15);
                    pkmn.EggGroup1 = (EggGroup)pokedata.ReadByte(0x16);
                    pkmn.EggGroup2 = (EggGroup)pokedata.ReadByte(0x17);
                    pkmn.Ability1 = (PBEAbility)pokedata.ReadByte(0x18);
                    pkmn.Ability2 = (PBEAbility)pokedata.ReadByte(0x19);
                    pkmn.AbilityH = (PBEAbility)pokedata.ReadByte(0x1A);
                    pkmn.FleeRate = pokedata.ReadByte(0x1B);
                    // 0x1C-0x1D - formid
                    // 0x1E-0x1F - form
                    // 0x20 - num forms
                    // 0x21 - color
                    pkmn.BaseEXPYield = pokedata.ReadUInt16(0x22);
                    // 0x24-0x25 - height
                    pkmn.Weight = Math.Round(pokedata.ReadUInt16(0x26) * 0.1, 1);
                    // 0x28-0x35 - tmhm
                    // 0x36-0x37 ?
                    // 0x38 - free tutor moves bitfield
                    // 0x39-0x3B ?
                    // 0x3C-0x4B - b2w2 tutor moves bitfield
                }
            }
        }
        private static void WritePokemonData(string dir, Pokemon pkmn)
        {
            using (var w = new JsonTextWriter(File.CreateText(Path.Combine(dir, "BaseStats.json"))) { Formatting = Formatting.Indented })
            {
                w.WriteStartObject();
                w.WritePropertyName(nameof(Pokemon.HP));
                w.WriteValue(pkmn.HP);
                w.WritePropertyName(nameof(Pokemon.Attack));
                w.WriteValue(pkmn.Attack);
                w.WritePropertyName(nameof(Pokemon.Defense));
                w.WriteValue(pkmn.Defense);
                w.WritePropertyName(nameof(Pokemon.SpAttack));
                w.WriteValue(pkmn.SpAttack);
                w.WritePropertyName(nameof(Pokemon.SpDefense));
                w.WriteValue(pkmn.SpDefense);
                w.WritePropertyName(nameof(Pokemon.Speed));
                w.WriteValue(pkmn.Speed);
                w.WritePropertyName(nameof(Pokemon.Type1));
                w.WriteValue(pkmn.Type1.ToString());
                w.WritePropertyName(nameof(Pokemon.Type2));
                w.WriteValue(pkmn.Type2.ToString());
                w.WritePropertyName(nameof(Pokemon.CatchRate));
                w.WriteValue(pkmn.CatchRate);
                w.WritePropertyName(nameof(Pokemon.BaseFriendship));
                w.WriteValue(pkmn.BaseFriendship);
                w.WritePropertyName(nameof(Pokemon.GenderRatio));
                w.WriteValue(pkmn.GenderRatio.ToString());
                w.WritePropertyName(nameof(Pokemon.GrowthRate));
                w.WriteValue(pkmn.GrowthRate.ToString());
                w.WritePropertyName(nameof(Pokemon.BaseEXPYield));
                w.WriteValue(pkmn.BaseEXPYield);
                w.WritePropertyName(nameof(Pokemon.EggGroup1));
                w.WriteValue(pkmn.EggGroup1.ToString());
                w.WritePropertyName(nameof(Pokemon.EggGroup2));
                w.WriteValue(pkmn.EggGroup2.ToString());
                w.WritePropertyName(nameof(Pokemon.Ability1));
                w.WriteValue(pkmn.Ability1.ToString());
                w.WritePropertyName(nameof(Pokemon.Ability2));
                w.WriteValue(pkmn.Ability2.ToString());
                w.WritePropertyName(nameof(Pokemon.AbilityH));
                w.WriteValue(pkmn.AbilityH.ToString());
                w.WritePropertyName(nameof(Pokemon.FleeRate));
                w.WriteValue(pkmn.FleeRate);
                w.WritePropertyName(nameof(Pokemon.Weight));
                w.WriteValue(pkmn.Weight);
                w.WriteEndObject();
            }
        }

        private static void ReadEvolutions(NARC b2w2Evolution)
        {
            for (int sp = 1; sp <= 708; sp++)
            {
                // Skip Egg, Bad Egg, and Pokéstar Studios Pokémon in B2W2
                if (sp > 649 && sp < 685)
                {
                    continue;
                }

                if (!_b2w2SpeciesIndexToPBESpecies.TryGetValue(sp, out (PBESpecies, PBEForm) key))
                {
                    key = ((PBESpecies)sp, 0);
                }
                using (var evolution = new EndianBinaryReader(new MemoryStream(b2w2Evolution[sp]), Endianness.LittleEndian))
                {
                    Pokemon pkmn = _dict[key];
                    if (pkmn.BabySpecies == 0)
                    {
                        pkmn.BabySpecies = key.Item1;
                    }
                    for (int i = 0; i < 7; i++)
                    {
                        var method = (EvoMethod)evolution.ReadUInt16();
                        ushort param = evolution.ReadUInt16();
                        var evo = (PBESpecies)evolution.ReadUInt16();
                        pkmn.Evolutions[i] = (method, param, evo, 0);
                        if (method != EvoMethod.None)
                        {
                            _dict[(evo, 0)].BabySpecies = pkmn.BabySpecies;
                        }
                    }
                }
            }
        }
        private static void WriteEvolutions(string dir, Pokemon pkmn)
        {
            using (var w = new JsonTextWriter(File.CreateText(Path.Combine(dir, "Evolutions.json"))) { Formatting = Formatting.Indented })
            {
                w.WriteStartObject();
                w.WritePropertyName(nameof(Pokemon.BabySpecies));
                w.WriteValue(pkmn.BabySpecies.ToString());
                w.WritePropertyName(nameof(Pokemon.Evolutions));
                w.WriteStartArray();
                for (int i = 0; i < 7; i++)
                {
                    (EvoMethod method, ushort param, PBESpecies species, PBEForm form) = pkmn.Evolutions[i];
                    if (method == EvoMethod.None)
                    {
                        continue;
                    }

                    w.WriteStartObject();
                    w.WritePropertyName("Method");
                    w.WriteValue(method.ToString());
                    switch (method)
                    {
                        case EvoMethod.Friendship_LevelUp:
                        case EvoMethod.Friendship_Day_LevelUp:
                        case EvoMethod.Friendship_Night_LevelUp:
                        {
                            w.WritePropertyName("FriendshipRequired");
                            w.WriteValue(param);
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
                            w.WritePropertyName("LevelRequired");
                            w.WriteValue(param);
                            break;
                        }
                        case EvoMethod.Beauty_LevelUp:
                        {
                            w.WritePropertyName("BeautyRequired");
                            w.WriteValue(param);
                            break;
                        }
                        case EvoMethod.Item_Trade:
                        case EvoMethod.Stone:
                        case EvoMethod.Male_Stone:
                        case EvoMethod.Female_Stone:
                        case EvoMethod.Item_Day_LevelUp:
                        case EvoMethod.Item_Night_LevelUp:
                        {
                            w.WritePropertyName("ItemRequired");
                            w.WriteValue(((PBEItem)param).ToString());
                            break;
                        }
                        case EvoMethod.Move_LevelUp:
                        {
                            w.WritePropertyName("MoveRequired");
                            w.WriteValue(((PBEMove)param).ToString());
                            break;
                        }
                        case EvoMethod.PartySpecies_LevelUp:
                        {
                            w.WritePropertyName("SpeciesRequired");
                            w.WriteValue(((PBESpecies)param).ToString());
                            break;
                        }
                    }
                    w.WritePropertyName("Species");
                    w.WriteValue(species.ToString());
                    w.WritePropertyName("Form");
                    w.WriteValue(PBEDataUtils.GetNameOfForm(species, form));
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                w.WriteEndObject();
            }
        }

        private static void ReadLevelUpMoves(NARC b2w2LevelUp)
        {
            for (int sp = 1; sp <= 708; sp++)
            {
                // Skip Egg, Bad Egg, and Pokéstar Studios Pokémon in B2W2
                if (sp > 649 && sp < 685)
                {
                    continue;
                }

                if (!_b2w2SpeciesIndexToPBESpecies.TryGetValue(sp, out (PBESpecies, PBEForm) key))
                {
                    key = ((PBESpecies)sp, 0);
                }
                using (var reader = new EndianBinaryReader(new MemoryStream(b2w2LevelUp[sp]), Endianness.LittleEndian))
                {
                    Pokemon pkmn = _dict[key];
                    while (true)
                    {
                        uint val = reader.ReadUInt32();
                        if (val == 0xFFFFFFFF)
                        {
                            break;
                        }
                        else
                        {
                            pkmn.LevelUpMoves.Add(((PBEMove)val, (byte)(val >> 0x10)));
                        }
                    }
                }
            }
        }
        private static void WriteLevelUpMoves(string dir, Pokemon pkmn)
        {
            using (var w = new JsonTextWriter(File.CreateText(Path.Combine(dir, "LevelUp.json"))) { Formatting = Formatting.Indented })
            {
                w.WriteStartObject();
                w.WritePropertyName(nameof(Pokemon.LevelUpMoves));
                w.WriteStartArray();
                foreach ((PBEMove move, byte level) in pkmn.LevelUpMoves)
                {
                    w.WriteStartObject();
                    w.Formatting = Formatting.None;
                    w.WritePropertyName("Move");
                    w.WriteValue(move.ToString());
                    w.WritePropertyName("Level");
                    w.WriteValue(level);
                    w.WriteEndObject();
                    w.Formatting = Formatting.Indented;
                }
                w.WriteEndArray();
                w.WriteEndObject();
            }
        }

        private static void ReadTMHMMoves(NARC b2w2Pokedata)
        {
            for (int sp = 1; sp <= 708; sp++)
            {
                // Skip Egg, Bad Egg, and Pokéstar Studios Pokémon in B2W2
                if (sp > 649 && sp < 685)
                {
                    continue;
                }

                if (!_b2w2SpeciesIndexToPBESpecies.TryGetValue(sp, out (PBESpecies, PBEForm) key))
                {
                    key = ((PBESpecies)sp, 0);
                }
                using (var reader = new EndianBinaryReader(new MemoryStream(b2w2Pokedata[sp]), Endianness.LittleEndian))
                {
                    byte[] bytes = reader.ReadBytes(13, 0x28);
                    for (int i = 0; i < _gen5TMHMs.Length; i++)
                    {
                        if ((bytes[i / 8] & (1 << (i % 8))) != 0)
                        {
                            PBEMoveObtainMethod flag;
                            if (i < 95)
                            {
                                flag = PBEMoveObtainMethod.TM_B2W2;
                            }
                            else
                            {
                                flag = PBEMoveObtainMethod.HM_BWB2W2;
                            }
                            AddOtherMove(key, _gen5TMHMs[i], flag);
                        }
                    }
                }
            }
        }

        private static void ReadMoveTutorMoves(NARC b2w2Pokedata)
        {
            for (int sp = 1; sp <= 708; sp++)
            {
                // Skip Egg, Bad Egg, and Pokéstar Studios Pokémon
                if (sp > 649 && sp < 685)
                {
                    continue;
                }

                if (!_b2w2SpeciesIndexToPBESpecies.TryGetValue(sp, out (PBESpecies, PBEForm) key))
                {
                    key = ((PBESpecies)sp, 0);
                }
                using (var reader = new EndianBinaryReader(new MemoryStream(b2w2Pokedata[sp]), Endianness.LittleEndian))
                {
                    // Free tutor moves
                    {
                        byte val = reader.ReadByte(0x38);
                        for (int i = 0; i < _gen5FreeTutorMoves.Length; i++)
                        {
                            if ((val & (1 << i)) != 0)
                            {
                                AddOtherMove(key, _gen5FreeTutorMoves[i], PBEMoveObtainMethod.MoveTutor_B2W2);
                            }
                        }
                    }
                    // B2W2 tutor moves
                    for (int i = 0; i < _b2w2TutorMoves.Length; i++)
                    {
                        uint val = reader.ReadUInt32(0x3C + (sizeof(uint) * i));
                        for (int j = 0; j < _b2w2TutorMoves[i].Length; j++)
                        {
                            if ((val & (1u << j)) != 0)
                            {
                                AddOtherMove(key, _b2w2TutorMoves[i][j], PBEMoveObtainMethod.MoveTutor_B2W2);
                            }
                        }
                    }
                }
            }
        }

        private static void ReadEggMoves(NARC bwb2w2Egg)
        {
            for (int sp = 1; sp <= 649; sp++)
            {
                using (var reader = new EndianBinaryReader(new MemoryStream(bwb2w2Egg[sp]), Endianness.LittleEndian))
                {
                    ushort numEggMoves = reader.ReadUInt16();
                    if (numEggMoves > 0)
                    {
                        Pokemon pkmn = _dict[((PBESpecies)sp, 0)];
                        pkmn.EggMoves = reader.ReadEnums<PBEMove>(numEggMoves);
                    }
                }
            }
        }
        private static void WriteEggMoves(string dir, Pokemon pkmn)
        {
            using (var w = new JsonTextWriter(File.CreateText(Path.Combine(dir, "EggMoves.json"))) { Formatting = Formatting.Indented })
            {
                w.WriteStartObject();
                w.WritePropertyName(nameof(Pokemon.EggMoves));
                w.WriteStartArray();
                foreach (PBEMove move in pkmn.EggMoves)
                {
                    w.WriteStartObject();
                    w.Formatting = Formatting.None;
                    w.WritePropertyName("Move");
                    w.WriteValue(move.ToString());
                    w.WriteEndObject();
                    w.Formatting = Formatting.Indented;
                }
                w.WriteEndArray();
                w.WriteEndObject();
            }
        }

        private static void FixForms()
        {
            static void CopySpecies((PBESpecies, PBEForm) baseKey, (PBESpecies, PBEForm) newKey)
            {
                Pokemon basePkmn = _dict[baseKey];
                Pokemon pkmn = AddSpecies(newKey);
                pkmn.Copy(basePkmn);
            }
            CopySpecies((PBESpecies.Burmy, PBEForm.Burmy_Plant), (PBESpecies.Burmy, PBEForm.Burmy_Sandy));
            CopySpecies((PBESpecies.Burmy, PBEForm.Burmy_Plant), (PBESpecies.Burmy, PBEForm.Burmy_Trash));
            CopySpecies((PBESpecies.Cherrim, PBEForm.Cherrim), (PBESpecies.Cherrim, PBEForm.Cherrim_Sunshine));
            CopySpecies((PBESpecies.Deerling, PBEForm.Deerling_Spring), (PBESpecies.Deerling, PBEForm.Deerling_Summer));
            CopySpecies((PBESpecies.Deerling, PBEForm.Deerling_Spring), (PBESpecies.Deerling, PBEForm.Deerling_Autumn));
            CopySpecies((PBESpecies.Deerling, PBEForm.Deerling_Spring), (PBESpecies.Deerling, PBEForm.Deerling_Winter));
            CopySpecies((PBESpecies.Gastrodon, PBEForm.Gastrodon_West), (PBESpecies.Gastrodon, PBEForm.Gastrodon_East));
            CopySpecies((PBESpecies.Genesect, PBEForm.Genesect), (PBESpecies.Genesect, PBEForm.Genesect_Douse));
            CopySpecies((PBESpecies.Genesect, PBEForm.Genesect), (PBESpecies.Genesect, PBEForm.Genesect_Shock));
            CopySpecies((PBESpecies.Genesect, PBEForm.Genesect), (PBESpecies.Genesect, PBEForm.Genesect_Burn));
            CopySpecies((PBESpecies.Genesect, PBEForm.Genesect), (PBESpecies.Genesect, PBEForm.Genesect_Chill));
            CopySpecies((PBESpecies.Sawsbuck, PBEForm.Sawsbuck_Spring), (PBESpecies.Sawsbuck, PBEForm.Sawsbuck_Summer));
            CopySpecies((PBESpecies.Sawsbuck, PBEForm.Sawsbuck_Spring), (PBESpecies.Sawsbuck, PBEForm.Sawsbuck_Autumn));
            CopySpecies((PBESpecies.Sawsbuck, PBEForm.Sawsbuck_Spring), (PBESpecies.Sawsbuck, PBEForm.Sawsbuck_Winter));
            CopySpecies((PBESpecies.Shellos, PBEForm.Shellos_West), (PBESpecies.Shellos, PBEForm.Shellos_East));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_B));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_C));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_D));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_E));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_F));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_G));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_H));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_I));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_J));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_K));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_L));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_M));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_N));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_O));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_P));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_Q));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_R));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_S));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_T));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_U));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_V));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_W));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_X));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_Y));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_Z));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_Exclamation));
            CopySpecies((PBESpecies.Unown, PBEForm.Unown_A), (PBESpecies.Unown, PBEForm.Unown_Question));
            _dict[(PBESpecies.Burmy, PBEForm.Burmy_Sandy)].Evolutions[0].Form = PBEForm.Wormadam_Sandy;
            _dict[(PBESpecies.Burmy, PBEForm.Burmy_Trash)].Evolutions[0].Form = PBEForm.Wormadam_Trash;
            _dict[(PBESpecies.Deerling, PBEForm.Deerling_Summer)].Evolutions[0].Form = PBEForm.Sawsbuck_Summer;
            _dict[(PBESpecies.Deerling, PBEForm.Deerling_Autumn)].Evolutions[0].Form = PBEForm.Sawsbuck_Autumn;
            _dict[(PBESpecies.Deerling, PBEForm.Deerling_Winter)].Evolutions[0].Form = PBEForm.Sawsbuck_Winter;
            _dict[(PBESpecies.Shellos, PBEForm.Shellos_East)].Evolutions[0].Form = PBEForm.Gastrodon_East;
            // Phione/Manaphy
            _dict[(PBESpecies.Manaphy, 0)].BabySpecies = PBESpecies.Phione;
        }
        private static void FixArceus()
        {
            Pokemon basePkmn = _dict[(PBESpecies.Arceus, PBEForm.Arceus)];
            void FixArceus(PBEForm form, PBEType type)
            {
                Pokemon pkmn = AddSpecies((PBESpecies.Arceus, form));
                pkmn.CopyArceus(basePkmn, type);
            }
            FixArceus(PBEForm.Arceus_Fighting, PBEType.Fighting);
            FixArceus(PBEForm.Arceus_Flying, PBEType.Flying);
            FixArceus(PBEForm.Arceus_Poison, PBEType.Poison);
            FixArceus(PBEForm.Arceus_Ground, PBEType.Ground);
            FixArceus(PBEForm.Arceus_Rock, PBEType.Rock);
            FixArceus(PBEForm.Arceus_Bug, PBEType.Bug);
            FixArceus(PBEForm.Arceus_Ghost, PBEType.Ghost);
            FixArceus(PBEForm.Arceus_Steel, PBEType.Steel);
            FixArceus(PBEForm.Arceus_Fire, PBEType.Fire);
            FixArceus(PBEForm.Arceus_Water, PBEType.Water);
            FixArceus(PBEForm.Arceus_Grass, PBEType.Grass);
            FixArceus(PBEForm.Arceus_Electric, PBEType.Electric);
            FixArceus(PBEForm.Arceus_Psychic, PBEType.Psychic);
            FixArceus(PBEForm.Arceus_Ice, PBEType.Ice);
            FixArceus(PBEForm.Arceus_Dragon, PBEType.Dragon);
            FixArceus(PBEForm.Arceus_Dark, PBEType.Dark);
        }
        private static void GiveFormsEggMoves()
        {
            // The only ones to be copied are Castform and Basculin
            foreach ((PBESpecies, PBEForm) key in _b2w2SpeciesIndexToPBESpecies.Values)
            {
                _dict[key].EggMoves = _dict[(key.Item1, 0)].EggMoves;
            }
        }
    }
}
