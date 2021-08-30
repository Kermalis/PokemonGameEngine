namespace Kermalis.PokemonGameEngine.Render
{
    internal struct AtlasPos
    {
        public readonly float AtlasStartX;
        public readonly float AtlasStartY;
        public readonly float AtlasEndX;
        public readonly float AtlasEndY;

        public AtlasPos(bool xFlip, bool yFlip)
        {
            AtlasStartX = xFlip ? 1 : 0;
            AtlasStartY = yFlip ? 1 : 0;
            AtlasEndX = xFlip ? 0 : 1;
            AtlasEndY = yFlip ? 0 : 1;
        }
        public AtlasPos(Rect2D rect, Size2D atlasSize, bool xFlip = false, bool yFlip = false)
        {
            AtlasStartX = Renderer.AbsXToRelX(xFlip ? rect.GetExclusiveRight() : rect.TopLeft.X, atlasSize.Width);
            AtlasStartY = Renderer.AbsYToRelY(yFlip ? rect.GetExclusiveBottom() : rect.TopLeft.Y, atlasSize.Height);
            AtlasEndX = Renderer.AbsXToRelX(xFlip ? rect.TopLeft.X : rect.GetExclusiveRight(), atlasSize.Width);
            AtlasEndY = Renderer.AbsYToRelY(yFlip ? rect.TopLeft.Y : rect.GetExclusiveBottom(), atlasSize.Height);
        }

        public RelPos2D GetTopLeft()
        {
            return new RelPos2D(AtlasStartX, AtlasStartY);
        }
        public RelPos2D GetBottomLeft()
        {
            return new RelPos2D(AtlasStartX, AtlasEndY);
        }
        public RelPos2D GetTopRight()
        {
            return new RelPos2D(AtlasEndX, AtlasStartY);
        }
        public RelPos2D GetBottomRight()
        {
            return new RelPos2D(AtlasEndX, AtlasEndY);
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("[SX: {0}, SY: {1}, EX: {2}, EY: {3}]", AtlasStartX, AtlasStartY, AtlasEndX, AtlasEndY);
        }
#endif
    }
}
