using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.DefaultData;
using Kermalis.PokemonGameEngine.Core;
using System;

namespace Kermalis.PokemonGameEngine.Item
{
    internal sealed class ItemData
    {
        public const int NUM_TM = 95;
        public const int NUM_HM = 6;

        #region TM HM lookups

        public static readonly PBEMove[] TMMoves = new PBEMove[NUM_TM]
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
        public static readonly PBEMove[] HMMoves = new PBEMove[NUM_HM]
        {
            PBEMove.Cut,
            PBEMove.Fly,
            PBEMove.Surf,
            PBEMove.Strength,
            PBEMove.Waterfall,
            PBEMove.Dive
        };

        #endregion

        /// <summary>Returns zero-indexed, or -1 if it's not a TM</summary>
        public static int GetTMIndex(ItemType item)
        {
            if (item >= ItemType.TM01 && item <= ItemType.TM92)
            {
                return item - ItemType.TM01; // 00 to 91
            }
            if (item >= ItemType.TM93 && item <= ItemType.TM95)
            {
                return 92 + (item - ItemType.TM93); // 92 to 94
            }
            return -1;
        }
        /// <summary>Returns zero-indexed, or -1 if it's not an HM</summary>
        public static int GetHMIndex(ItemType item)
        {
            if (item >= ItemType.HM01 && item <= ItemType.HM06)
            {
                return item - ItemType.HM01; // 0 to 5
            }
            return -1;
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
        public static void Debug_GiveAllItems<T>(Inventory<T> pi) where T : InventorySlot
        {
            foreach (ItemType item in Enum.GetValues<ItemType>())
            {
                if (item == ItemType.None || item == ItemType.MAX)
                {
                    continue;
                }
                pi.Add(item, 1);
            }
        }

        private static string GetItemIconOverride(ItemType item)
        {
            // Handle TMs and HMs first
            int index = GetTMIndex(item);
            if (index != -1)
            {
                return "TM_" + PBEDataProvider.Instance.GetMoveData(TMMoves[index], cache: false).Type;
            }
            index = GetHMIndex(item);
            if (index != -1)
            {
                return "HM_" + PBEDataProvider.Instance.GetMoveData(HMMoves[index], cache: false).Type;
            }
            // Both DNA Splicer items use the same icon
            if (item is ItemType.DNASplicers1 or ItemType.DNASplicers2)
            {
                return "DNASplicers";
            }
            // All data cards use the same icon
            if (item >= ItemType.DataCard01 && item <= ItemType.DataCard27)
            {
                return "DataCard";
            }
            // Dropped items use Xtransceiver icons
            if (item == ItemType.DroppedItemMale)
            {
                return "XtransceiverMale";
            }
            if (item == ItemType.DroppedItemFemale)
            {
                return "XtransceiverFemale";
            }
            // Invalid items
            if (item == ItemType.None || item == ItemType.MAX || !Enum.IsDefined(item))
            {
                return "None";
            }
            // Valid items
            return item.ToString();
        }
        public static string GetItemIconAssetPath(ItemType item)
        {
            return AssetLoader.GetPath(@"Sprites\Item Icons\" + GetItemIconOverride(item) + ".png");
        }

        // TODO: For now, manually assigning items I'm testing with to correct pockets, and anything else is free space
        public static ItemPouchType GetPouchType(ItemType item)
        {
            if (item is ItemType.OvalCharm or ItemType.ShinyCharm)
            {
                return ItemPouchType.KeyItems;
            }
            if (GetTMIndex(item) != -1 || GetHMIndex(item) != -1)
            {
                return ItemPouchType.TMHMs;
            }
            if (item is ItemType.XSpDef)
            {
                return ItemPouchType.BattleItems;
            }
            if (item.ToString().EndsWith("Berry"))
            {
                return ItemPouchType.Berries;
            }
            if (item is not (ItemType.SmokeBall or ItemType.LightBall or ItemType.IronBall)
                && item.ToString().EndsWith("Ball"))
            {
                return ItemPouchType.Balls;
            }
            if (item is not ItemType.SecretPotion
                && item.ToString().EndsWith("Potion"))
            {
                return ItemPouchType.Medicine;
            }
            return ItemPouchType.FreeSpace;
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
            if (item is ItemType.DNASplicers1 or ItemType.DNASplicers2)
            {
                return "DNA Splicers";
            }
            if (item is ItemType.XtransceiverMale or ItemType.XtransceiverFemale)
            {
                return "Xtransceiver";
            }
            if (item is ItemType.DroppedItemMale or ItemType.DroppedItemFemale)
            {
                return "Dropped Item";
            }

            var pbe = (PBEItem)item;
            if (!Enum.IsDefined(pbe))
            {
                return item.ToString();
            }
            return PBEDDLocalizedString.GetItemName(pbe).English;
        }
    }
}
