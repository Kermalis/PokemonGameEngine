using Kermalis.PokemonGameEngine.World;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed partial class Blockset
    {
        public sealed partial class Block
        {
            public class Tile
            {
                private readonly bool _xFlip;
                private readonly bool _yFlip;
                public readonly Tileset.Tile TilesetTile; // There are usually more blockset tiles than tileset tiles, so storing the tileset here would just waste memory

                public Tile(bool xFlip, bool yFlip, Tileset.Tile tilesetTile)
                {
                    _xFlip = xFlip;
                    _yFlip = yFlip;
                    TilesetTile = tilesetTile;
                }

                public virtual UV GetUV()
                {
                    return MakeUV(TilesetTile.Id);
                }

                protected UV MakeUV(int id)
                {
                    Tileset tileset = TilesetTile.Tileset;
                    var tilePixel = new Vec2I(id % tileset.NumTilesX * Overworld.Tile_NumPixelsX, id / tileset.NumTilesX * Overworld.Tile_NumPixelsY);
                    return new UV(Rect.FromSize(tilePixel, Overworld.Tile_NumPixels), tileset.TextureSize, xFlip: _xFlip, yFlip: _yFlip);
                }
            }

            public sealed class AnimatedTile : Tile
            {
                public AnimatedTile(bool xFlip, bool yFlip, Tileset.Tile tilesetTile)
                    : base(xFlip, yFlip, tilesetTile)
                {
                }

                public override UV GetUV()
                {
                    int id = ((Tileset.AnimatedTile)TilesetTile).AnimId;
                    if (id == TileAnimation.NO_ANIM_ID)
                    {
                        id = TilesetTile.Id;
                    }
                    return MakeUV(id);
                }
            }
        }
    }
}
