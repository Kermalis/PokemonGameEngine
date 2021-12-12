using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.World;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Data
{
    internal static class TilesetAnimationLoader
    {
        private const string FILE = @"Tileset\Animation\Animations.bin";

        private static readonly Dictionary<int, uint[]> _offsets; // Tileset, offsets[]

        static TilesetAnimationLoader()
        {
            using (EndianBinaryReader r = GetReader())
            {
                int numTilesets = r.ReadInt32();
                _offsets = new Dictionary<int, uint[]>(numTilesets);
                for (int i = 0; i < numTilesets; i++)
                {
                    int tilesetId = r.ReadInt32();
                    int numAnims = r.ReadInt32();
                    uint[] anims = r.ReadUInt32s(numAnims);
                    _offsets.Add(tilesetId, anims);
                }
            }
        }
        private static EndianBinaryReader GetReader()
        {
            return new EndianBinaryReader(AssetLoader.GetAssetStream(FILE), encoding: EncodingType.UTF16);
        }

        public static TileAnimation[] Load(Tileset t)
        {
            if (!_offsets.TryGetValue(t.Id, out uint[] offsets))
            {
                return null;
            }

            using (EndianBinaryReader r = GetReader())
            {
                var arr = new TileAnimation[offsets.Length];
                for (int i = 0; i < offsets.Length; i++)
                {
                    r.BaseStream.Position = offsets[i];
                    arr[i] = new TileAnimation(t, new TileAnimationData(r));
                }
                return arr;
            }
        }
    }
}
