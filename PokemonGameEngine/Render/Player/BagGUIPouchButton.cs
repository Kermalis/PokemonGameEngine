using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;

namespace Kermalis.PokemonGameEngine.Render.Player
{
    internal sealed class BagGUIPouchButton
    {
        private readonly Vec2I _pos;
        private readonly Image _icon;

        private float _selectedTimer;

        public BagGUIPouchButton(ItemPouchType pouch, Vec2I pos)
        {
            _pos = pos;
            _icon = Image.LoadOrGet(AssetLoader.GetPath(@"Sprites\Item Pouches\" + pouch + ".png"));
        }

        public void Render(bool isSelected)
        {
            _icon.Render(_pos);
            if (isSelected)
            {
                _selectedTimer += Display.DeltaTime;
                // Animate selection color
                uint green = (uint)Utils.Lerp(150f, 200f, Easing.BellCurve2(_selectedTimer % 1f));
                GUIRenderer.Rect(Colors.V4FromRGB(80, green, 255), Rect.FromSize(_pos, new Vec2I(24, 24)), lineThickness: 2, cornerRadii: new(4));
            }
            else
            {
                _selectedTimer = 0f;
            }
        }

        public void Delete()
        {
            _icon.DeductReference();
        }
    }
}
