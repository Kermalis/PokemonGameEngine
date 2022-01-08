using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render
{
    internal struct UV
    {
        public readonly Vector2 Start;
        public readonly Vector2 End;

        public UV(bool xFlip, bool yFlip)
        {
            Start.X = xFlip ? 1f : 0f;
            Start.Y = yFlip ? 1f : 0f;
            End.X = xFlip ? 0f : 1f;
            End.Y = yFlip ? 0f : 1f;
        }
        public UV(in Rect rect, Vec2I atlasSize, bool xFlip = false, bool yFlip = false)
        {
            Start.X = (float)(xFlip ? rect.GetExclusiveRight() : rect.TopLeft.X) / atlasSize.X;
            Start.Y = (float)(yFlip ? rect.GetExclusiveBottom() : rect.TopLeft.Y) / atlasSize.Y;
            End.X = (float)(xFlip ? rect.TopLeft.X : rect.GetExclusiveRight()) / atlasSize.X;
            End.Y = (float)(yFlip ? rect.TopLeft.Y : rect.GetExclusiveBottom()) / atlasSize.Y;
        }

        public static UV FromAtlas(int imgIndex, Vec2I imgSize, Vec2I atlasSize, bool xFlip = false, bool yFlip = false)
        {
            int i = imgIndex * imgSize.X;
            Vec2I topLeft;
            topLeft.X = i % atlasSize.X;
            topLeft.Y = i / atlasSize.X * imgSize.Y;

            var rect = Rect.FromSize(topLeft, imgSize);
            return new UV(rect, atlasSize, xFlip: xFlip, yFlip: yFlip);
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
