using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Kermalis.PokemonGameEngine.Render
{
    internal sealed class WriteableBitmapSurface : IFramebufferPlatformSurface
    {
        private readonly WriteableBitmap _bitmap;
        public WriteableBitmapSurface(WriteableBitmap bmp)
        {
            _bitmap = bmp;
        }
        public ILockedFramebuffer Lock()
        {
            return _bitmap.Lock();
        }
    }
}
