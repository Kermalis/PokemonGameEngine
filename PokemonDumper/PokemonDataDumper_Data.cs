using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonDumper
{
    internal static partial class PokemonDataDumper
    {
        private enum EvoMethod : ushort
        {
            None, // [no param]
            Friendship_LevelUp, // (param = friendship)
            Friendship_Day_LevelUp, // (param = friendship)
            Friendship_Night_LevelUp, // (param = friendship)
            LevelUp, // (param = level)
            Trade, // [no param]
            Item_Trade, // (param = item)
            ShelmetKarrablast, // [no param]
            Stone, // (param = item)
            ATK_GT_DEF_LevelUp, // (param = level)
            ATK_EE_DEF_LevelUp, // (param = level)
            ATK_LT_DEF_LevelUp, // (param = level)
            Silcoon_LevelUp, // (param = level)
            Cascoon_LevelUp, // (param = level)
            Ninjask_LevelUp, // (param = level)
            Shedinja_LevelUp, // (param = level)
            Beauty_LevelUp, // (param = beauty amount)
            Male_Stone, // (param = item)
            Female_Stone, // (param = item)
            Item_Day_LevelUp, // (param = item)
            Item_Night_LevelUp, // (param = item)
            Move_LevelUp, // (param = move)
            PartySpecies_LevelUp, // (param = species)
            Male_LevelUp, // (param = level)
            Female_LevelUp, // (param = level)
            NosepassMagneton_Location_LevelUp, // [no param]
            Leafeon_Location_LevelUp, // [no param]
            Glaceon_Location_LevelUp, // [no param]
            MAX
        }
        private enum EggGroup : byte
        {
            Invalid,
            Monster,
            Water1,
            Bug,
            Flying,
            Field,
            Fairy,
            Grass,
            HumanLike,
            Water3,
            Mineral,
            Amorphous,
            Water2,
            Ditto,
            Dragon,
            Undiscovered,
            MAX
        }
        private sealed class Pokemon
        {
            public byte HP;
            public byte Attack;
            public byte Defense;
            public byte SpAttack;
            public byte SpDefense;
            public byte Speed;
            public PBEType Type1;
            public PBEType Type2;
            public PBEGenderRatio GenderRatio;
            public PBEGrowthRate GrowthRate;
            public byte BaseFriendship;
            public ushort BaseEXPYield;
            public byte CatchRate;
            public byte FleeRate;
            public double Weight;
            public EggGroup EggGroup1;
            public EggGroup EggGroup2;
            public PBEAbility Ability1;
            public PBEAbility Ability2;
            public PBEAbility AbilityH;
            public List<(PBEMove Move, byte Level)> LevelUpMoves = new List<(PBEMove, byte)>();
            public PBEMove[] EggMoves = Array.Empty<PBEMove>();
            public Dictionary<PBEMove, PBEMoveObtainMethod> OtherMoves = new Dictionary<PBEMove, PBEMoveObtainMethod>();
            public PBESpecies BabySpecies;
            public (EvoMethod Method, ushort Param, PBESpecies Species, PBEForm Form)[] Evolutions = new (EvoMethod, ushort, PBESpecies, PBEForm)[7];

            public void Copy(Pokemon other)
            {
                HP = other.HP;
                Attack = other.Attack;
                Defense = other.Defense;
                SpAttack = other.SpAttack;
                SpDefense = other.SpDefense;
                Speed = other.Speed;
                Type1 = other.Type1;
                Type2 = other.Type2;
                GenderRatio = other.GenderRatio;
                GrowthRate = other.GrowthRate;
                BaseFriendship = other.BaseFriendship;
                BaseEXPYield = other.BaseEXPYield;
                EggGroup1 = other.EggGroup1;
                EggGroup2 = other.EggGroup2;
                Ability1 = other.Ability1;
                Ability2 = other.Ability2;
                AbilityH = other.AbilityH;
                CatchRate = other.CatchRate;
                FleeRate = other.FleeRate;
                Weight = other.Weight;
                BabySpecies = other.BabySpecies;
                Evolutions = ((EvoMethod, ushort, PBESpecies, PBEForm)[])other.Evolutions.Clone();
                LevelUpMoves = other.LevelUpMoves;
                EggMoves = other.EggMoves;
                OtherMoves = other.OtherMoves;
            }
            public void CopyArceus(Pokemon other, PBEType type)
            {
                HP = other.HP;
                Attack = other.Attack;
                Defense = other.Defense;
                SpAttack = other.SpAttack;
                SpDefense = other.SpDefense;
                Speed = other.Speed;
                Type1 = type;
                Type2 = other.Type2;
                GenderRatio = other.GenderRatio;
                GrowthRate = other.GrowthRate;
                BaseFriendship = other.BaseFriendship;
                BaseEXPYield = other.BaseEXPYield;
                EggGroup1 = other.EggGroup1;
                EggGroup2 = other.EggGroup2;
                Ability1 = other.Ability1;
                Ability2 = other.Ability2;
                AbilityH = other.AbilityH;
                CatchRate = other.CatchRate;
                FleeRate = other.FleeRate;
                Weight = other.Weight;
                BabySpecies = other.BabySpecies;
                Evolutions = other.Evolutions;
                LevelUpMoves = other.LevelUpMoves;
                EggMoves = other.EggMoves;
                OtherMoves = other.OtherMoves;
            }
        }

        private static readonly PBEType[] _gen5Types = new PBEType[17]
        {
            PBEType.Normal,
            PBEType.Fighting,
            PBEType.Flying,
            PBEType.Poison,
            PBEType.Ground,
            PBEType.Rock,
            PBEType.Bug,
            PBEType.Ghost,
            PBEType.Steel,
            PBEType.Fire,
            PBEType.Water,
            PBEType.Grass,
            PBEType.Electric,
            PBEType.Psychic,
            PBEType.Ice,
            PBEType.Dragon,
            PBEType.Dark
        };

        private static readonly Dictionary<int, (PBESpecies, PBEForm)> _b2w2SpeciesIndexToPBESpecies = new Dictionary<int, (PBESpecies, PBEForm)>
        {
            { 685, (PBESpecies.Deoxys, PBEForm.Deoxys_Attack) },
            { 686, (PBESpecies.Deoxys, PBEForm.Deoxys_Defense) },
            { 687, (PBESpecies.Deoxys, PBEForm.Deoxys_Speed) },
            { 688, (PBESpecies.Wormadam, PBEForm.Wormadam_Sandy) },
            { 689, (PBESpecies.Wormadam, PBEForm.Wormadam_Trash) },
            { 690, (PBESpecies.Shaymin, PBEForm.Shaymin_Sky) },
            { 691, (PBESpecies.Giratina, PBEForm.Giratina_Origin) },
            { 692, (PBESpecies.Rotom, PBEForm.Rotom_Heat) },
            { 693, (PBESpecies.Rotom, PBEForm.Rotom_Wash) },
            { 694, (PBESpecies.Rotom, PBEForm.Rotom_Frost) },
            { 695, (PBESpecies.Rotom, PBEForm.Rotom_Fan) },
            { 696, (PBESpecies.Rotom, PBEForm.Rotom_Mow) },
            { 697, (PBESpecies.Castform, PBEForm.Castform_Sunny) },
            { 698, (PBESpecies.Castform, PBEForm.Castform_Rainy) },
            { 699, (PBESpecies.Castform, PBEForm.Castform_Snowy) },
            { 700, (PBESpecies.Basculin, PBEForm.Basculin_Blue) },
            { 701, (PBESpecies.Darmanitan, PBEForm.Darmanitan_Zen) },
            { 702, (PBESpecies.Meloetta, PBEForm.Meloetta_Pirouette) },
            { 703, (PBESpecies.Kyurem, PBEForm.Kyurem_White) },
            { 704, (PBESpecies.Kyurem, PBEForm.Kyurem_Black) },
            { 705, (PBESpecies.Keldeo, PBEForm.Keldeo_Resolute) },
            { 706, (PBESpecies.Tornadus, PBEForm.Tornadus_Therian) },
            { 707, (PBESpecies.Thundurus, PBEForm.Thundurus_Therian) },
            { 708, (PBESpecies.Landorus, PBEForm.Landorus_Therian) }
        };
        private static readonly PBEMove[] _gen5TMHMs = new PBEMove[101]
        {
            PBEMove.HoneClaws,
            PBEMove.DragonClaw,
            PBEMove.Psyshock,
            PBEMove.CalmMind,
            PBEMove.Roar,
            PBEMove.Toxic,
            PBEMove.Hail,
            PBEMove.BulkUp,
            PBEMove.Venoshock,
            PBEMove.HiddenPower,
            PBEMove.SunnyDay,
            PBEMove.Taunt,
            PBEMove.IceBeam,
            PBEMove.Blizzard,
            PBEMove.HyperBeam,
            PBEMove.LightScreen,
            PBEMove.Protect,
            PBEMove.RainDance,
            PBEMove.Telekinesis,
            PBEMove.Safeguard,
            PBEMove.Frustration,
            PBEMove.SolarBeam,
            PBEMove.SmackDown,
            PBEMove.Thunderbolt,
            PBEMove.Thunder,
            PBEMove.Earthquake,
            PBEMove.Return,
            PBEMove.Dig,
            PBEMove.Psychic,
            PBEMove.ShadowBall,
            PBEMove.BrickBreak,
            PBEMove.DoubleTeam,
            PBEMove.Reflect,
            PBEMove.SludgeWave,
            PBEMove.Flamethrower,
            PBEMove.SludgeBomb,
            PBEMove.Sandstorm,
            PBEMove.FireBlast,
            PBEMove.RockTomb,
            PBEMove.AerialAce,
            PBEMove.Torment,
            PBEMove.Facade,
            PBEMove.FlameCharge,
            PBEMove.Rest,
            PBEMove.Attract,
            PBEMove.Thief,
            PBEMove.LowSweep,
            PBEMove.Round,
            PBEMove.EchoedVoice,
            PBEMove.Overheat,
            PBEMove.AllySwitch,
            PBEMove.FocusBlast,
            PBEMove.EnergyBall,
            PBEMove.FalseSwipe,
            PBEMove.Scald,
            PBEMove.Fling,
            PBEMove.ChargeBeam,
            PBEMove.SkyDrop,
            PBEMove.Incinerate,
            PBEMove.Quash,
            PBEMove.WillOWisp,
            PBEMove.Acrobatics,
            PBEMove.Embargo,
            PBEMove.Explosion,
            PBEMove.ShadowClaw,
            PBEMove.Payback,
            PBEMove.Retaliate,
            PBEMove.GigaImpact,
            PBEMove.RockPolish,
            PBEMove.Flash,
            PBEMove.StoneEdge,
            PBEMove.VoltSwitch,
            PBEMove.ThunderWave,
            PBEMove.GyroBall,
            PBEMove.SwordsDance,
            PBEMove.StruggleBug,
            PBEMove.PsychUp,
            PBEMove.Bulldoze,
            PBEMove.FrostBreath,
            PBEMove.RockSlide,
            PBEMove.XScissor,
            PBEMove.DragonTail,
            PBEMove.WorkUp,
            PBEMove.PoisonJab,
            PBEMove.DreamEater,
            PBEMove.GrassKnot,
            PBEMove.Swagger,
            PBEMove.Pluck,
            PBEMove.Uturn,
            PBEMove.Substitute,
            PBEMove.FlashCannon,
            PBEMove.TrickRoom,
            PBEMove.WildCharge,
            PBEMove.RockSmash,
            PBEMove.Snarl,
            PBEMove.Cut,
            PBEMove.Fly,
            PBEMove.Surf,
            PBEMove.Strength,
            PBEMove.Waterfall,
            PBEMove.Dive
        };
        private static readonly PBEMove[] _gen5FreeTutorMoves = new PBEMove[7]
        {
            PBEMove.GrassPledge,
            PBEMove.FirePledge,
            PBEMove.WaterPledge,
            PBEMove.FrenzyPlant,
            PBEMove.BlastBurn,
            PBEMove.HydroCannon,
            PBEMove.DracoMeteor
        };
        // These tutor moves are decompressed to memory (ram address 0x021D0B38 in B2, 0x021D0B6C in W2) on each map load (USA offsets)
        // For some reason, the location order in this table is different from the Pokémon's compatibility (this table is [Humilau,Driftveil,Nacrene,Lentimas] but in Pokémon data it is [Driftveil,Lentimas,Humilau,Nacrene])
        // Each tutor move entry is 0xC bytes:
        // u32 moveId
        // u32 shardCost
        // u32 indexInList
        private static readonly PBEMove[][] _b2w2TutorMoves = new PBEMove[4][]
        {
            new PBEMove[15] // Driftveil City
            {
                PBEMove.BugBite,
                PBEMove.Covet,
                PBEMove.SuperFang,
                PBEMove.DualChop,
                PBEMove.SignalBeam,
                PBEMove.IronHead,
                PBEMove.SeedBomb,
                PBEMove.DrillRun,
                PBEMove.Bounce,
                PBEMove.LowKick,
                PBEMove.GunkShot,
                PBEMove.Uproar,
                PBEMove.ThunderPunch,
                PBEMove.FirePunch,
                PBEMove.IcePunch
            },
            new PBEMove[17] // Lentimas Town
            {
                PBEMove.MagicCoat,
                PBEMove.Block,
                PBEMove.EarthPower,
                PBEMove.FoulPlay,
                PBEMove.Gravity,
                PBEMove.MagnetRise,
                PBEMove.IronDefense,
                PBEMove.LastResort,
                PBEMove.Superpower,
                PBEMove.Electroweb,
                PBEMove.IcyWind,
                PBEMove.AquaTail,
                PBEMove.DarkPulse,
                PBEMove.ZenHeadbutt,
                PBEMove.DragonPulse,
                PBEMove.HyperVoice,
                PBEMove.IronTail
            },
            new PBEMove[13] // Humilau City
            {
                PBEMove.Bind,
                PBEMove.Snore,
                PBEMove.KnockOff,
                PBEMove.Synthesis,
                PBEMove.HeatWave,
                PBEMove.RolePlay,
                PBEMove.HealBell,
                PBEMove.Tailwind,
                PBEMove.SkyAttack,
                PBEMove.PainSplit,
                PBEMove.GigaDrain,
                PBEMove.DrainPunch,
                PBEMove.Roost
            },
            new PBEMove[15] // Nacrene City
            {
                PBEMove.GastroAcid,
                PBEMove.WorrySeed,
                PBEMove.Spite,
                PBEMove.AfterYou,
                PBEMove.HelpingHand,
                PBEMove.Trick,
                PBEMove.MagicRoom,
                PBEMove.WonderRoom,
                PBEMove.Endeavor,
                PBEMove.Outrage,
                PBEMove.Recycle,
                PBEMove.Snatch,
                PBEMove.StealthRock,
                PBEMove.SleepTalk,
                PBEMove.SkillSwap
            }
        };
    }
}
