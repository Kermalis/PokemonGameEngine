using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using System;

namespace Kermalis.PokemonGameEngine.Item
{
    internal sealed class ItemData
    {
        public const int NumTMs = 95;
        public const int NumHMs = 6;
        #region TM HM lookups
        private static readonly PBEMove[] _tmMoves = new PBEMove[NumTMs]
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
            PBEMove.Snarl
        };
        private static readonly PBEMove[] _hmMoves = new PBEMove[NumHMs]
        {
            PBEMove.Cut,
            PBEMove.Fly,
            PBEMove.Surf,
            PBEMove.Strength,
            PBEMove.Waterfall,
            PBEMove.Dive
        };
        #endregion

        public static bool IsTM(ItemType item)
        {
            return (item >= ItemType.TM01 && item <= ItemType.TM92)
                || (item >= ItemType.TM93 && item <= ItemType.TM95);
        }
        public static bool IsHM(ItemType item)
        {
            return item >= ItemType.HM01 && item <= ItemType.HM06;
        }
        /// <summary>Returns zero-indexed</summary>
        public static int GetTMIndex(ItemType item)
        {
            if (item >= ItemType.TM01 && item <= ItemType.TM92)
            {
                return item - ItemType.TM01;
            }
            return item - ItemType.TM93;
        }
        /// <summary>Returns zero-indexed</summary>
        public static int GetHMIndex(ItemType item)
        {
            return item - ItemType.HM01;
        }
        public static ItemType GetTMItem(int index)
        {
            if (index < 92)
            {
                return (ItemType)((int)ItemType.TM01 + index);
            }
            return (ItemType)((int)ItemType.TM93 + (index - 92));
        }
        public static ItemType GetHMItem(int index)
        {
            return (ItemType)((int)ItemType.HM01 + index);
        }
        public static PBEMove GetTMMove(ItemType item)
        {
            return _tmMoves[GetTMIndex(item)];
        }
        public static PBEMove GetHMMove(ItemType item)
        {
            return _hmMoves[GetHMIndex(item)];
        }
        public static void Debug_GiveAllTMHMs(PlayerInventory pi)
        {
            for (int i = 0; i < NumTMs; i++)
            {
                pi.Add(GetTMItem(i));
            }
            for (int i = 0; i < NumHMs; i++)
            {
                pi.Add(GetHMItem(i));
            }
        }

        public static ItemPouchType GetPouchType(ItemType item)
        {
            if (item == ItemType.OvalCharm || item == ItemType.ShinyCharm)
            {
                return ItemPouchType.KeyItems; // TODO: For now, oval and shiny charm are key items
            }
            if (IsTM(item) || IsHM(item))
            {
                return ItemPouchType.TMHMs;
            }
            return ItemPouchType.Items; // TODO
        }

        public static PBEStat? GetPowerItemStat(ItemType item)
        {
            switch (item)
            {
                case ItemType.PowerAnklet: return PBEStat.Speed;
                case ItemType.PowerBand: return PBEStat.SpDefense;
                case ItemType.PowerBelt: return PBEStat.Defense;
                case ItemType.PowerBracer: return PBEStat.Attack;
                case ItemType.PowerLens: return PBEStat.SpAttack;
                case ItemType.PowerWeight: return PBEStat.HP;
            }
            return null;
        }

        // temporary
        public static string GetItemName(ItemType item)
        {
            var pbe = (PBEItem)item;
            if (!Enum.IsDefined(typeof(PBEItem), pbe))
            {
                return item.ToString();
            }
            return PBELocalizedString.GetItemName(pbe).English;
        }
    }
}
