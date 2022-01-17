namespace Kermalis.PokemonGameEngine.Render.Images
{
    internal interface IImage
    {
        uint Texture { get; }
        Vec2I Size { get; }

        void DeductReference();
    }
}
