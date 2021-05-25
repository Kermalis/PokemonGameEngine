using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using System.Linq;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class DaycarePokemon
    {
        public readonly BoxPokemon Pkmn;
        public byte LevelsGained;

        public DaycarePokemon(PartyPokemon pkmn)
        {
            Pkmn = new BoxPokemon(pkmn);
        }

        public void IncrementStep()
        {
            if (Pkmn.Level >= PkmnConstants.MaxLevel)
            {
                return; // Cannot level up anymore
            }

            var bs = BaseStats.Get(Pkmn.Species, Pkmn.Form, true);
            PBEGrowthRate growthRate = bs.GrowthRate;
            uint nextLevelAmt = PBEDataProvider.Instance.GetEXPRequired(growthRate, (byte)(Pkmn.Level + 1));
            if (++Pkmn.EXP < nextLevelAmt)
            {
                return; // No level up
            }
            Pkmn.Level++;
            LevelsGained++;
            TeachNewMoves();
        }

#if DEBUG
        public void Debug_LevelUpManually(int numTimes)
        {
            var bs = BaseStats.Get(Pkmn.Species, Pkmn.Form, true);
            PBEGrowthRate growthRate = bs.GrowthRate;
            for (int i = 0; i < numTimes; i++)
            {
                uint nextLevelAmt = PBEDataProvider.Instance.GetEXPRequired(growthRate, (byte)(Pkmn.Level + 1));
                Pkmn.EXP = nextLevelAmt;
                Pkmn.Level++;
                LevelsGained++;
                TeachNewMoves();
            }
        }
#endif

        private void TeachNewMoves()
        {
            var lvlUpData = new LevelUpData(Pkmn.Species, Pkmn.Form);
            PBEMove[] newMoves = lvlUpData.GetNewMoves(Pkmn.Level).Reverse().Take(PkmnConstants.NumMoves).ToArray();
            BoxMoveset moveset = Pkmn.Moveset;
            for (int i = 0; i < newMoves.Length; i++)
            {
                int firstEmpty = moveset.GetFirstEmptySlot();
                BoxMoveset.BoxMovesetSlot slot;
                if (firstEmpty != -1)
                {
                    slot = moveset[firstEmpty];
                }
                else
                {
                    moveset.ShiftMovesUp();
                    slot = moveset[PkmnConstants.NumMoves - 1];
                }
                slot.Move = newMoves[i];
                slot.PPUps = 0;
            }
        }
    }
}
