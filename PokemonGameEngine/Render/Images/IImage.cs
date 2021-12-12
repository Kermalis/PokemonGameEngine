﻿namespace Kermalis.PokemonGameEngine.Render.Images
{
    internal interface IImage
    {
        uint Texture { get; }
        Size2D Size { get; }

        void DeductReference();
    }
}
