using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    internal sealed class PartyGUIMember
    {
        private readonly bool _usePartyPkmn;
        private uint _color;
        private readonly PartyPokemon _partyPkmn;
        private readonly SpritedBattlePokemon _battlePkmn;
        private readonly Sprite _mini;
        private readonly Image _background;

        public PartyGUIMember(PartyPokemon pkmn, SpriteList sprites)
        {
            _usePartyPkmn = true;
            _partyPkmn = pkmn;
            _color = GetColor();
            _mini = new Sprite()
            {
                Image = PokemonImageUtils.GetMini(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny, pkmn.IsEgg),
                Y = Sprite_BounceDefY,
                Callback = Sprite_Bounce,
                Data = new Sprite_BounceData()
            };
            sprites.Add(_mini);
            _background = new Image((UI.Program.RenderWidth / 2) - (UI.Program.RenderWidth / 20), (UI.Program.RenderHeight / 4) - (UI.Program.RenderHeight / 20));
            UpdateBackground();
        }
        public PartyGUIMember(SpritedBattlePokemon pkmn, SpriteList sprites)
        {
            _usePartyPkmn = false;
            _battlePkmn = pkmn;
            _color = GetColor();
            _mini = new Sprite()
            {
                Image = pkmn.Mini,
                Y = Sprite_BounceDefY,
                Callback = Sprite_Bounce,
                Data = new Sprite_BounceData()
            };
            sprites.Add(_mini);
            _background = new Image((UI.Program.RenderWidth / 2) - (UI.Program.RenderWidth / 20), (UI.Program.RenderHeight / 4) - (UI.Program.RenderHeight / 20));
            UpdateBackground();
        }

        #region Sprite Callbacks

        private const int Sprite_BounceMinY = 6;
        private const int Sprite_BounceDefY = 7;
        private const int Sprite_BounceMaxY = 8;
        private class Sprite_BounceData { public bool Down = true; public int Target = Sprite_BounceMaxY; public int Speed = 1; public int Counter = 0; }
        private static void Sprite_Bounce(Sprite s)
        {
            var data = (Sprite_BounceData)s.Data;
            if (data.Counter++ < 1)
            {
                return;
            }
            data.Counter = 0;
            if (data.Down)
            {
                s.Y += data.Speed;
                if (s.Y >= data.Target)
                {
                    s.Y = data.Target;
                    data.Down = false;
                    data.Target = Sprite_BounceMinY;
                }
            }
            else
            {
                s.Y -= data.Speed;
                if (s.Y <= data.Target)
                {
                    s.Y = data.Target;
                    data.Down = true;
                    data.Target = Sprite_BounceMaxY;
                }
            }
        }

        public void SetBigBounce()
        {
            ((Sprite_BounceData)_mini.Data).Speed = 2;
        }
        public void SetSmallBounce()
        {
            ((Sprite_BounceData)_mini.Data).Speed = 1;
        }

        #endregion

        public unsafe void UpdateBackground()
        {
            _background.Draw(DrawBackground);
        }
        private unsafe void DrawBackground(uint* dst, int dstW, int dstH)
        {
            Renderer.OverwriteRectangle(dst, dstW, dstH, _color);
            // Shadow
            Renderer.FillEllipse_Points(dst, dstW, dstH, 3, 34, 29, 39, Renderer.Color(0, 0, 0, 100));
            // Nickname
            PartyPokemon p = _usePartyPkmn ? _partyPkmn : _battlePkmn.PartyPkmn;
            Font.DefaultSmall.DrawString(dst, dstW, dstH, 2, 3, p.Nickname, Font.DefaultWhite_I);
            if (p.IsEgg)
            {
                return;
            }
            PBEBattlePokemon bPkmn = _usePartyPkmn ? null : _battlePkmn.Pkmn;
            // Gender
            PBEGender gender = p.Gender;
            if (gender != PBEGender.Genderless)
            {
                Font.Default.DrawString(dst, dstW, dstH, 61, -2, gender.ToSymbol(), gender == PBEGender.Male ? Font.DefaultBlue_O : Font.DefaultRed_O);
            }
            // Level
            const int lvX = 72;
            Font.PartyNumbers.DrawString(dst, dstW, dstH, lvX, 3, "[LV]", Font.DefaultWhite_I);
            Font.PartyNumbers.DrawString(dst, dstW, dstH, lvX + 12, 3, (_usePartyPkmn ? p.Level : bPkmn.Level).ToString(), Font.DefaultWhite_I);
            // Status
            PBEStatus1 status = _usePartyPkmn ? p.Status1 : bPkmn.Status1;
            if (status != PBEStatus1.None)
            {
                Font.DefaultSmall.DrawString(dst, dstW, dstH, 61, 13, status.ToString(), Font.DefaultWhite_I);
            }
            // Item
            ItemType item = _usePartyPkmn ? p.Item : (ItemType)bPkmn.Item;
            if (item != ItemType.None)
            {
                Font.DefaultSmall.DrawString(dst, dstW, dstH, 61, 23, ItemData.GetItemName(item), Font.DefaultWhite_I);
            }
        }

        // We shouldn't be redrawing on the logic thread, but it currently doesn't matter
        public void UpdateColorAndRedraw()
        {
            _color = GetColor();
            UpdateBackground();
        }
        private uint GetColor()
        {
            if (_usePartyPkmn)
            {
                PartyPokemon pp = _partyPkmn;
                if (!pp.IsEgg && pp.HP == 0)
                {
                    return GetFaintedColor();
                }
                return GetDefaultColor();
            }

            SpritedBattlePokemon s = _battlePkmn;
            if (!s.PartyPkmn.IsEgg && s.Pkmn.HP == 0)
            {
                return GetFaintedColor();
            }
            if (s.Pkmn.FieldPosition != PBEFieldPosition.None)
            {
                return GetActiveColor();
            }
            if (BattleGUI.Instance.StandBy.Contains(s.Pkmn))
            {
                return GetStandByColor();
            }
            return GetDefaultColor();
        }
        private static uint GetDefaultColor()
        {
            return Renderer.Color(48, 48, 48, 128);
        }
        private static uint GetFaintedColor()
        {
            return Renderer.Color(120, 30, 60, 196);
        }
        private static uint GetActiveColor()
        {
            return Renderer.Color(255, 192, 60, 96);
        }
        private static uint GetStandByColor()
        {
            return Renderer.Color(125, 255, 195, 100);
        }

        public unsafe void Render(uint* dst, int dstW, int dstH, int x, int y, bool selected)
        {
            _background.DrawOn(dst, dstW, dstH, x, y);
            if (selected)
            {
                Renderer.DrawRectangle(dst, dstW, dstH, x, y, _background.Width, _background.Height, Renderer.Color(48, 180, 255, 200));
            }
            _mini.DrawOn(dst, dstW, dstH, xOffset: x, yOffset: y);
        }
    }
}
