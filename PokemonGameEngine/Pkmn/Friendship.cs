using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.World;
using System.Collections.Generic;

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

        private static readonly Dictionary<Event, sbyte[]> _eventBonuses = new Dictionary<Event, sbyte[]>
        {
            { Event.Walking, new sbyte[3] { +2, +2, +1 } },
            { Event.LevelUpBattle, new sbyte[3] { +5, +4, +3 } },
            { Event.Vitamin, new sbyte[3] { +5, +3, +2 } },
            { Event.Wing, new sbyte[3] { +3, +2, +1 } },
            { Event.TMHM, new sbyte[3] { +1, +1, +0 } },
            { Event.BattleItem, new sbyte[3] { +1, +1, +0 } },
            { Event.Faint_L30, new sbyte[3] { -1, -1, -1 } },
            { Event.Faint_GE30, new sbyte[3] { -5, -5, -10 } },
            { Event.Powder, new sbyte[3] { -5, -5, -10 } },
            { Event.EnergyRoot, new sbyte[3] { -10, -10, -15 } },
            { Event.RevivalHerb, new sbyte[3] { -15, -15, -20 } },
            { Event.FriendshipBerry, new sbyte[3] { +10, +5, +2 } },
            { Event.LeagueBattle, new sbyte[3] { +5, +4, +3 } }
        };

        public static void AdjustFriendship(PartyPokemon pkmn, Event e)
        {
            if (e == Event.Walking && PBEDataProvider.GlobalRandom.RandomBool())
            {
                return; // 50% chance walking boosts
            }

            int friendshipLevel = 0;
            int friendship = pkmn.Friendship;
            if (friendship >= 100)
            {
                friendshipLevel++;
            }
            if (friendship >= 200)
            {
                friendshipLevel++;
            }

            int mod = _eventBonuses[e][friendshipLevel];
            if (mod > 0)
            {
                if (pkmn.CaughtBall == PBEItem.LuxuryBall)
                {
                    mod++;
                }
                if (pkmn.MetLocation == Overworld.GetCurrentLocation())
                {
                    mod++;
                }
                if (pkmn.Item == PBEItem.SootheBell)
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
#if DEBUG
            System.Console.WriteLine("{0} friendship adjustment: {1}_{2}", pkmn.Nickname, e, mod);
#endif
            pkmn.Friendship = (byte)friendship;
        }

        public static void UpdateFriendshipStep()
        {
            short val = Game.Instance.Save.Vars[Var.Friendship_Step_Counter];
            val++;
            val %= 128;
            Game.Instance.Save.Vars[Var.Friendship_Step_Counter] = val;
            if (val == 0)
            {
                foreach (PartyPokemon p in Game.Instance.Save.PlayerParty)
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
