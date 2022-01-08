using Kermalis.PokemonGameEngine.World.Data;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed class TileAnimation
    {
        public const int NO_ANIM_ID = -1;

        private readonly TileAnimationData _data;

        private float _time;

        public TileAnimation(in TileAnimationData data)
        {
            _data = data;
        }

        public bool ContainsTile(int tileId)
        {
            TileAnimationData.Frame[] frames = _data.Frames;
            for (int j = 0; j < frames.Length; j++)
            {
                if (frames[j].TilesetTile == tileId)
                {
                    return true;
                }
            }
            return false;
        }

        public void Update(Tileset tileset)
        {
            float t = (_time + Display.DeltaTime) % _data.Duration;
            _time = t;
            for (int f = 0; f < _data.Frames.Length; f++)
            {
                ref TileAnimationData.Frame frame = ref _data.Frames[f];
                var tile = (Tileset.AnimatedTile)tileset.Tiles[frame.TilesetTile];
                int prevId = tile.AnimId;
                for (int s = frame.Stops.Length - 1; s >= 0; s--)
                {
                    ref TileAnimationData.Frame.Stop stop = ref frame.Stops[s];
                    if (stop.Time <= t)
                    {
                        tile.AnimId = stop.AnimTile;
                        goto bottom;
                    }
                }
                tile.AnimId = NO_ANIM_ID;
            bottom:
                if (tile.AnimId != prevId)
                {
                    tile.IsDirty = true;
                }
            }
        }
        public void FinishUpdate(Tileset tileset)
        {
            TileAnimationData.Frame[] frames = _data.Frames;
            for (int j = 0; j < frames.Length; j++)
            {
                var tile = (Tileset.AnimatedTile)tileset.Tiles[frames[j].TilesetTile];
                tile.IsDirty = false;
            }
        }
    }
}
