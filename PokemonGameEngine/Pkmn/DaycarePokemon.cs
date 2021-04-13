using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using System.Linq;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    internal sealed class DaycarePokemon
    {
        public readonly BoxPokemon Pkmn;
        private byte _levelsGained;

        public DaycarePokemon(PartyPokemon pkmn)
        {
            Pkmn = new BoxPokemon(pkmn);
        }

        public void IncrementStep()
        {
            // TODO: Exp level logic, learn new moves, etc
            //LevelsGained++;

            bool leveledUp = false;
            if (!leveledUp)
            {
                return;
            }
            // New move logic
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
