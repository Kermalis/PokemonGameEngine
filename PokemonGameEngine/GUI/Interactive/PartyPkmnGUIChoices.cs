using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Interactive
{
    internal sealed class PartyPkmnGUIChoice : GUIChoice
    {
        public bool IsDirty;

        private readonly PartyPokemon _pkmn;
        private readonly Image _mini;

        private Image _drawn;
        public override bool IsSelected
        {
            get => base.IsSelected;
            set
            {
                base.IsSelected = value;
                if (_drawn != null) // initial add means _drawn is null
                {
                    Draw();
                }
            }
        }

        public PartyPkmnGUIChoice(PartyPokemon pkmn, Action command, bool isEnabled = true)
            : base(command, isEnabled: isEnabled)
        {
            _pkmn = pkmn;
            _mini = PokemonImageUtils.GetMini(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny, pkmn.IsEgg);

            IsDirty = true;
        }

        public void UpdateSize(int width, int height)
        {
            _drawn = new Image(width, height);
            Draw();
        }
        public unsafe void Draw()
        {
            _drawn.Draw(Draw);
        }
        private unsafe void Draw(uint* dst, int dstW, int dstH)
        {
            uint backColor = IsSelected ? Renderer.Color(200, 200, 200, 255) : Renderer.Color(255, 255, 255, 255);
            Renderer.FillRoundedRectangle(dst, dstW, dstH, 0, 0, dstW - 1, dstH - 1, dstH / 2, backColor);

            _mini.DrawOn(dst, dstW, dstH, 0f, -0.15f);

            Font fontDefault = Font.Default;
            uint[] defaultDark = Font.DefaultDarkGray_I;

            fontDefault.DrawString(dst, dstW, dstH, 0.2f, 0.01f, _pkmn.Nickname, defaultDark);

            if (_pkmn.IsEgg)
            {
                return; // Eggs don't show the rest
            }

            Font fontPartyNumbers = Font.PartyNumbers;
            fontPartyNumbers.DrawString(dst, dstW, dstH, 0.2f, 0.65f, _pkmn.HP + "/" + _pkmn.MaxHP, defaultDark);
            fontPartyNumbers.DrawString(dst, dstW, dstH, 0.7f, 0.65f, "[LV] " + _pkmn.Level, defaultDark);
            PBEGender gender = _pkmn.Gender;
            if (gender != PBEGender.Genderless)
            {
                fontDefault.DrawString(dst, dstW, dstH, 0.7f, 0.01f, gender.ToSymbol(), gender == PBEGender.Male ? Font.DefaultBlue_O : Font.DefaultRed_O);
            }

            Renderer.FillRectangle_Points(dst, dstW, dstH, 0.2f, 0.58f, 0.7f, 0.64f, Renderer.Color(99, 255, 99, 255));
        }

        public unsafe void Render(uint* dst, int dstW, int dstH, int x, int y)
        {
            _drawn.DrawOn(dst, dstW, dstH, x, y);
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

        public override unsafe void Render(uint* dst, int dstW, int dstH)
        {
            int x1 = (int)(X * dstW);
            float fy1 = Y * dstH;

            float fHeight = (_y2 - Y) / PkmnConstants.PartyCapacity * dstH;
            if (_dirtySizes)
            {
                _dirtySizes = false;
                int width = (int)((_x2 - X) * dstW);
                int height = (int)fHeight;
                foreach (PartyPkmnGUIChoice c in _choices)
                {
                    if (c.IsDirty)
                    {
                        c.UpdateSize(width, height);
                    }
                }
            }
            float space = Spacing * dstH;
            int count = _choices.Count;
            for (int i = 0; i < count; i++)
            {
                PartyPkmnGUIChoice c = _choices[i];
                int y = (int)(fy1 + (fHeight * i) + (space * i));
                c.Render(dst, dstW, dstH, x1, y);
            }
        }
    }
}
