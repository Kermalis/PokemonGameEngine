#if DEBUG_FRIENDSHIP
using Kermalis.PokemonGameEngine.Debug;
#endif
using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.World;
using System;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal static class Friendship
    {
        public enum Event : byte
        {
            Walking,
            LevelUpBattle,
            Vitamin,
            Wing,
            TMHM,
            BattleItem,
            Faint_L30,
            Faint_GE30,
            Powder,
            EnergyRoot,
            RevivalHerb,
            FriendshipBerry,
            LeagueBattle
        }

        private static sbyte[] GetEventBonuses(Event e)
        {
            switch (e)
            {
                case Event.Walking: return new sbyte[3] { +2, +2, +1 };
                case Event.LevelUpBattle: return new sbyte[3] { +5, +4, +3 };
                case Event.Vitamin: return new sbyte[3] { +5, +3, +2 };
                case Event.Wing: return new sbyte[3] { +3, +2, +1 };
                case Event.TMHM: return new sbyte[3] { +1, +1, +0 };
                case Event.BattleItem: return new sbyte[3] { +1, +1, +0 };
                case Event.Faint_L30: return new sbyte[3] { -1, -1, -1 };
                case Event.Faint_GE30: return new sbyte[3] { -5, -5, -10 };
                case Event.Powder: return new sbyte[3] { -5, -5, -10 };
                case Event.EnergyRoot: return new sbyte[3] { -10, -10, -15 };
                case Event.RevivalHerb: return new sbyte[3] { -15, -15, -20 };
                case Event.FriendshipBerry: return new sbyte[3] { +10, +5, +2 };
                case Event.LeagueBattle: return new sbyte[3] { +5, +4, +3 };
            }
            throw new Exception();
        }

        private static byte GetAdjustedFriendship(Event e, byte curFriendship, ItemType caughtBall, MapSection metLocation, ItemType item
#if DEBUG_FRIENDSHIP
            , string nickname
#endif
            )
        {
            if (e == Event.Walking && PBEDataProvider.GlobalRandom.RandomBool())
            {
                return curFriendship; // 50% chance walking boosts
            }

            int friendshipLevel = 0;
            int friendship = curFriendship;
            if (friendship >= 100)
            {
                friendshipLevel++;
            }
            if (friendship >= 200)
            {
                friendshipLevel++;
            }

            int mod = GetEventBonuses(e)[friendshipLevel];
            if (mod > 0)
            {
                if (caughtBall == ItemType.LuxuryBall)
                {
                    mod++;
                }
                if (metLocation == Overworld.GetCurrentLocation())
                {
                    mod++;
                }
                if (item == ItemType.SootheBell)
                {
                    mod = (int)(mod * 1.5f);
                }
            }
            friendship += mod;
            if (friendship < 0)
            {
                friendship = 0;
            }
            if (friendship > byte.MaxValue)
            {
                friendship = byte.MaxValue;
            }
#if DEBUG_FRIENDSHIP
            Log.WriteLineWithTime(string.Format("{0} friendship adjustment: {1}_{2}", nickname, e, mod));
#endif
            return (byte)friendship;
        }
        public static void AdjustFriendship(PBEBattlePokemon bPkmn, PartyPokemon pkmn, Event e)
        {
            bPkmn.Friendship = GetAdjustedFriendship(e, bPkmn.Friendship, pkmn.CaughtBall, pkmn.MetLocation, (ItemType)bPkmn.Item
#if DEBUG_FRIENDSHIP
                , pkmn.Nickname
#endif
                );
        }
        public static void AdjustFriendship(PartyPokemon pkmn, Event e)
        {
            pkmn.Friendship = GetAdjustedFriendship(e, pkmn.Friendship, pkmn.CaughtBall, pkmn.MetLocation, pkmn.Item
#if DEBUG_FRIENDSHIP
                , pkmn.Nickname
#endif
                );
        }

        public static void UpdateFriendshipStep()
        {
            short val = Engine.Instance.Save.Vars[Var.Friendship_Step_Counter];
            val++;
            val %= 128;
            Engine.Instance.Save.Vars[Var.Friendship_Step_Counter] = val;
            if (val == 0)
            {
                foreach (PartyPokemon p in Engine.Instance.Save.PlayerParty)
                {
                    if (!p.IsEgg)
                    {
                        AdjustFriendship(p, Event.Walking);
                    }
                }
            }
        }
    }
}
