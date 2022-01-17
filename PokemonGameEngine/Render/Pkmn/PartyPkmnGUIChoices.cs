using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Pkmn
{
    internal sealed class PartyPkmnGUIChoice : GUIChoice
    {
        public bool IsDirty;

        private readonly PartyPokemon _pkmn;
        private readonly Image _mini;

        private FrameBuffer2DColor _frameBuffer;
        public override bool IsSelected
        {
            get => base.IsSelected;
            set
            {
                base.IsSelected = value;
                if (_frameBuffer is not null) // initial add means _frameBuffer is null
                {
                    Draw();
                }
            }
        }

        public PartyPkmnGUIChoice(PartyPokemon pkmn, Action command, bool isEnabled = true)
            : base(command, isEnabled: isEnabled)
        {
            _pkmn = pkmn;
            if (pkmn.IsEgg)
            {
                _mini = PokemonImageLoader.GetEggMini();
            }
            else
            {
                _mini = PokemonImageLoader.GetMini(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny);
            }

            IsDirty = true;
        }

        public void UpdateSize(Vec2I size)
        {
            _frameBuffer?.Delete();
            _frameBuffer = new FrameBuffer2DColor(size);
            Draw();
        }
        public void Draw()
        {
            GL gl = Display.OpenGL;
            _frameBuffer.Use(gl);
            Vec2I totalSize = _frameBuffer.Size;

            Vector4 backColor = IsSelected ? Colors.V4FromRGB(200, 200, 200) : Colors.White4;
            GUIRenderer.Rect(backColor, Rect.FromSize(new Vec2I(0, 0), totalSize), cornerRadius: totalSize.Y / 2);

            _mini.Render(Vec2I.FromRelative(0f, -0.15f, totalSize));

            Font fontDefault = Font.Default;
            Vector4[] defaultDark = FontColors.DefaultDarkGray_I;
            GUIString.CreateAndRenderOneTimeString(_pkmn.Nickname, fontDefault, defaultDark, Vec2I.FromRelative(0.2f, 0.01f, totalSize));

            if (_pkmn.IsEgg)
            {
                return; // Eggs don't show the rest
            }

            Font fontPartyNumbers = Font.PartyNumbers;
            GUIString.CreateAndRenderOneTimeString(_pkmn.HP + "/" + _pkmn.MaxHP, fontPartyNumbers, defaultDark, Vec2I.FromRelative(0.2f, 0.65f, totalSize));
            GUIString.CreateAndRenderOneTimeString("[LV] " + _pkmn.Level, fontPartyNumbers, defaultDark, Vec2I.FromRelative(0.7f, 0.65f, totalSize));
            GUIString.CreateAndRenderOneTimeGenderString(_pkmn.Gender, fontDefault, Vec2I.FromRelative(0.7f, 0.01f, totalSize));

            GUIRenderer.Rect(Colors.V4FromRGB(99, 255, 99), Rect.FromCorners(Vec2I.FromRelative(0.2f, 0.58f, totalSize), Vec2I.FromRelative(0.7f, 0.64f, totalSize)));
        }

        public void Render(Vec2I pos)
        {
            _frameBuffer.RenderColorTexture(pos);
        }

        public override void Dispose()
        {
            Command = null;
            _frameBuffer?.Delete();
            _mini.DeductReference();
        }
    }

    internal sealed class PartyPkmnGUIChoices : GUIChoices<PartyPkmnGUIChoice>
    {
        private bool _dirtySizes;

        private readonly Vector2 BottomRight;

        public PartyPkmnGUIChoices(Vector2 topLeft, Vector2 bottomRight, float spacing,
            Action backCommand = null)
            : base(topLeft, spacing, backCommand: backCommand)
        {
            BottomRight = bottomRight;
            _dirtySizes = true;
        }

        public override void Add(PartyPkmnGUIChoice c)
        {
            base.Add(c);
            _dirtySizes = true;
        }

        public override void Render(Vec2I viewSize)
        {
            int x1 = (int)(Pos.X * viewSize.X);
            float fy1 = Pos.Y * viewSize.Y;

            float fHeight = (BottomRight.Y - Pos.Y) / PkmnConstants.PartyCapacity * viewSize.Y;
            if (_dirtySizes)
            {
                _dirtySizes = false;
                var size = new Vec2I((int)((BottomRight.X - Pos.X) * viewSize.X), (int)fHeight);
                foreach (PartyPkmnGUIChoice c in _choices)
                {
                    if (c.IsDirty)
                    {
                        c.UpdateSize(size);
                    }
                }
            }
            float space = Spacing * viewSize.Y;
            int count = _choices.Count;
            for (int i = 0; i < count; i++)
            {
                PartyPkmnGUIChoice c = _choices[i];
                int y = (int)(fy1 + (fHeight * i) + (space * i));
                c.Render(new Vec2I(x1, y));
            }
        }
    }
}
