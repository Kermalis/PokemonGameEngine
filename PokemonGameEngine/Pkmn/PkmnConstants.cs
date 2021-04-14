using Kermalis.PokemonBattleEngine.Data;

namespace Kermalis.PokemonGameEngine.Pkmn
{
    public static class PkmnConstants
    {
        public const int PartyCapacity = 6;
        public const int NumMoves = 4;
        public const int MinLevel = 1;
        public const int MaxLevel = 100;

        #region Boxes
        public const int NumBoxes = 27;
        public const int BoxCapacity = 25;
        #endregion

        public static readonly PBESettings PBESettings;
        static PkmnConstants()
        {
            PBESettings = new PBESettings
            {
                MaxPartySize = PartyCapacity,
                NumMoves = NumMoves,
                MinLevel = MinLevel,
                MaxLevel = MaxLevel,
            };
            PBESettings.MakeReadOnly();
        }
    }
}
