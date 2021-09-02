namespace Kermalis.PokemonGameEngine.Render
{
    internal struct AtlasPos
    {
        public readonly RelPos2D Start;
        public readonly RelPos2D End;

        public AtlasPos(bool xFlip, bool yFlip)
        {
            Start.X = xFlip ? 1 : 0;
            Start.Y = yFlip ? 1 : 0;
            End.X = xFlip ? 0 : 1;
            End.Y = yFlip ? 0 : 1;
        }
        public AtlasPos(in Rect2D rect, Size2D atlasSize, bool xFlip = false, bool yFlip = false)
        {
            Start.X = Renderer.AbsXToRelX(xFlip ? rect.GetExclusiveRight() : rect.TopLeft.X, atlasSize.Width);
            Start.Y = Renderer.AbsYToRelY(yFlip ? rect.GetExclusiveBottom() : rect.TopLeft.Y, atlasSize.Height);
            End.X = Renderer.AbsXToRelX(xFlip ? rect.TopLeft.X : rect.GetExclusiveRight(), atlasSize.Width);
            End.Y = Renderer.AbsYToRelY(yFlip ? rect.TopLeft.Y : rect.GetExclusiveBottom(), atlasSize.Height);
        }

        public RelPos2D GetBottomLeft()
        {
            return new RelPos2D(Start.X, End.Y);
        }
        public RelPos2D GetTopRight()
        {
            return new RelPos2D(End.X, Start.Y);
        }
        public RelPos2D GetBottomRight()
        {
            return new RelPos2D(End.X, End.Y);
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("[Start: {0}, End: {1}]", Start, End);
        }
#endif
    }
}
