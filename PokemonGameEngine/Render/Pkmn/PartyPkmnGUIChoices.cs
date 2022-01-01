using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Pkmn
{
    internal sealed class PartyPkmnGUIChoice : GUIChoice
    {
        public bool IsDirty;

        private readonly PartyPokemon _pkmn;
        private readonly Image _mini;

        private WriteableImage _drawn;
        public override bool IsSelected
        {
            get => base.IsSelected;
            set
            {
                base.IsSelected = value;
                if (_drawn is not null) // initial add means _drawn is null
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

        public void UpdateSize(Size2D size)
        {
            _drawn = new WriteableImage(size);
            Draw();
        }
        public void Draw()
        {
            FrameBuffer oldFBO = FrameBuffer.Current;
            _drawn.FrameBuffer.Use();
            Size2D totalSize = _drawn.Size;

            Vector4 backColor = IsSelected ? Colors.V4FromRGB(200, 200, 200) : Colors.White4;
            GUIRenderer.Instance.FillRectangle(backColor, new Rect2D(new Pos2D(0, 0), totalSize)); // TODO: Rounded of size (dstH / 2)

            _mini.Render(Pos2D.FromRelative(0f, -0.15f, totalSize));

            Font fontDefault = Font.Default;
            Vector4[] defaultDark = FontColors.DefaultDarkGray_I;
            GUIString.CreateAndRenderOneTimeString(_pkmn.Nickname, fontDefault, defaultDark, Pos2D.FromRelative(0.2f, 0.01f, totalSize));

            if (_pkmn.IsEgg)
            {
                goto bottom; // Eggs don't show the rest
            }

            Font fontPartyNumbers = Font.PartyNumbers;
            GUIString.CreateAndRenderOneTimeString(_pkmn.HP + "/" + _pkmn.MaxHP, fontPartyNumbers, defaultDark, Pos2D.FromRelative(0.2f, 0.65f, totalSize));
            GUIString.CreateAndRenderOneTimeString("[LV] " + _pkmn.Level, fontPartyNumbers, defaultDark, Pos2D.FromRelative(0.7f, 0.65f, totalSize));
            GUIString.CreateAndRenderOneTimeGenderString(_pkmn.Gender, fontDefault, Pos2D.FromRelative(0.7f, 0.01f, totalSize));

            GUIRenderer.Instance.FillRectangle(Colors.V4FromRGB(99, 255, 99), new Rect2D(Pos2D.FromRelative(0.2f, 0.58f, totalSize), Pos2D.FromRelative(0.7f, 0.64f, totalSize)));

        bottom:
            oldFBO.Use();
        }

        public void Render(Pos2D pos)
        {
            _drawn.Render(pos);
        }

        public override void Dispose()
        {
            Command = null;
            _drawn?.DeductReference();
            _mini.DeductReference();
        }
    }

    internal sealed class PartyPkmnGUIChoices : GUIChoices<PartyPkmnGUIChoice>
    {
        private bool _dirtySizes;

        private float _x2;
        public float X2
        {
            get => _x2;
            set
            {
                if (value != _x2)
                {
                    _x2 = value;
                    _dirtySizes = true;
                }
            }
        }
        private float _y2;
        public float Y2
        {
            get => _y2;
            set
            {
                if (value != _y2)
                {
                    _y2 = value;
                    _dirtySizes = true;
                }
            }
        }

        public PartyPkmnGUIChoices(float x1, float y1, float x2, float y2, float spacing,
            Action backCommand = null)
            : base(x1, y1, spacing, backCommand: backCommand)
        {
            _x2 = x2;
            _y2 = y2;
            _dirtySizes = true;
        }

        public override void Add(PartyPkmnGUIChoice c)
        {
            base.Add(c);
            _dirtySizes = true;
        }

        public override void Render()
        {
            Size2D dstSize = FrameBuffer.Current.Size;
            int x1 = (int)(X * dstSize.Width);
            float fy1 = Y * dstSize.Height;

            float fHeight = (_y2 - Y) / PkmnConstants.PartyCapacity * dstSize.Height;
            if (_dirtySizes)
            {
                _dirtySizes = false;
                var size = new Size2D((uint)((_x2 - X) * dstSize.Width), (uint)fHeight);
                foreach (PartyPkmnGUIChoice c in _choices)
                {
                    if (c.IsDirty)
                    {
                        c.UpdateSize(size);
                    }
                }
            }
            float space = Spacing * dstSize.Height;
            int count = _choices.Count;
            for (int i = 0; i < count; i++)
            {
                PartyPkmnGUIChoice c = _choices[i];
                int y = (int)(fy1 + (fHeight * i) + (space * i));
                c.Render(new Pos2D(x1, y));
            }
        }
    }
}
