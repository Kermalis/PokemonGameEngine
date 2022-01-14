using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;

namespace Kermalis.PokemonGameEngine.Render
{
    internal delegate void SpriteCallback(Sprite sprite);

    internal sealed class Sprite : IConnectedListObject<Sprite>
    {
        public Sprite Next { get; set; }
        public Sprite Prev { get; set; }

        public IImage Image;
        /// <summary>After this is updated, a call will need to be made to <see cref="SpriteList.SortByPriority"/>. Higher priorities are rendered last to appear above everything else</summary>
        public int Priority;
        public Vec2I Pos;
        public bool IsInvisible;
        public bool XFlip;
        public bool YFlip;

        public object Data;
        public object Tag;
        public SpriteCallback Callback;

        public void Render(Vec2I translation = default)
        {
            if (IsInvisible)
            {
                return;
            }

            IImage img = Image;
            GUIRenderer.Texture(img.Texture, Rect.FromSize(Pos + translation, img.Size), new UV(XFlip, YFlip));
        }

        public static int Sorter(Sprite s1, Sprite s2)
        {
            if (s1.Priority < s2.Priority)
            {
                return -1;
            }
            if (s1.Priority == s2.Priority)
            {
                return 0;
            }
            return 1;
        }

        public void Dispose()
        {
            // Do not dispose next or prev so we can continue looping after this gets removed
            Data = null;
            Callback = null;
            Image?.DeductReference();
            Image = null;
        }
    }
}
