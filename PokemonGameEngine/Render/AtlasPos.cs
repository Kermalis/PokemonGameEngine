using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render
{
    internal struct AtlasPos
    {
        public readonly Vector2 Start;
        public readonly Vector2 End;

        public AtlasPos(bool xFlip, bool yFlip)
        {
            Start.X = xFlip ? 1 : 0;
            Start.Y = yFlip ? 1 : 0;
            End.X = xFlip ? 0 : 1;
            End.Y = yFlip ? 0 : 1;
        }
        public AtlasPos(in Rect2D rect, Size2D atlasSize, bool xFlip = false, bool yFlip = false)
        {
            Start.X = (float)(xFlip ? rect.GetExclusiveRight() : rect.TopLeft.X) / atlasSize.Width;
            Start.Y = (float)(yFlip ? rect.GetExclusiveBottom() : rect.TopLeft.Y) / atlasSize.Height;
            End.X = (float)(xFlip ? rect.TopLeft.X : rect.GetExclusiveRight()) / atlasSize.Width;
            End.Y = (float)(yFlip ? rect.TopLeft.Y : rect.GetExclusiveBottom()) / atlasSize.Height;
        }

        public Vector2 GetBottomLeft()
        {
            return new Vector2(Start.X, End.Y);
        }
        public Vector2 GetTopRight()
        {
            return new Vector2(End.X, Start.Y);
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("[Start: {0}, End: {1}]", Start, End);
        }
#endif
    }
}
