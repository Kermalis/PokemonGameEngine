using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.GUIs;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Player
{
    internal sealed class BagGUITextButton
    {
        private readonly Rect _rect;
        private readonly GUIString _str;
        public Action OnPress;

        private float _selectedTimer;

        public BagGUITextButton(string str, Vec2I strPos, Rect rect, Action onPress)
        {
            OnPress = onPress;
            _rect = rect;
            _str = new GUIString(str, Font.Default, FontColors.DefaultWhite_I, pos: rect.TopLeft + strPos);
        }

        public void Render(bool isSelected)
        {
            Vector4 lineColor;
            if (isSelected)
            {
                _selectedTimer += Display.DeltaTime;
                // Animate selection color
                uint red = (uint)Utils.Lerp(170f, 220f, Easing.BellCurve2(_selectedTimer % 1f));
                lineColor = Colors.V4FromRGB(red, 50, 50);
            }
            else
            {
                _selectedTimer = 0f;
                lineColor = Colors.V4FromRGB(50, 50, 50);
            }
            GUIRenderer.Rect(Colors.V4FromRGB(75, 35, 215), lineColor, _rect, 2, cornerRadii: new(5));

            _str.Render();
        }

        public void Delete()
        {
            _str.Delete();
            OnPress = null;
        }
    }
}
