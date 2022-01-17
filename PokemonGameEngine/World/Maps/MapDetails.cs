using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;

namespace Kermalis.PokemonGameEngine.World.Maps
{
    internal sealed class MapDetails
    {
        public readonly MapFlags Flags;
        public readonly MapSection Section;
        public readonly MapWeather Weather;
        public readonly Song Music;
        public readonly PBEForm BurmyForm;

        public MapDetails(EndianBinaryReader r)
        {
            Flags = r.ReadEnum<MapFlags>();
            Section = r.ReadEnum<MapSection>();
            Weather = r.ReadEnum<MapWeather>();
            Music = r.ReadEnum<Song>();
            BurmyForm = r.ReadEnum<PBEForm>();
        }
    }
}
