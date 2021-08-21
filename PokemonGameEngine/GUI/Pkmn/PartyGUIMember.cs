using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    internal sealed class PartyGUIMember
    {
        private readonly bool _usePartyPkmn;
        private readonly bool _isEgg;
        private ColorF _color;
        private readonly PartyPokemon _partyPkmn;
        private readonly SpritedBattlePokemon _battlePkmn;
        private readonly Sprite _mini;
        private readonly WriteableImage _background;

        public PartyGUIMember(PartyPokemon pkmn, SpriteList sprites)
        {
            _usePartyPkmn = true;
            _isEgg = pkmn.IsEgg;
            _partyPkmn = pkmn;
            _color = GetColor();
            _mini = new Sprite()
            {
                Image = PokemonImageLoader.GetMini(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny, pkmn.IsEgg),
                Pos = new Pos2D(0, Sprite_BounceDefY)
            };
            if (!_isEgg)
            {
                _mini.Callback = Sprite_Bounce;
                _mini.Data = new Sprite_BounceData();
            }
            sprites.Add(_mini);
            _background = new WriteableImage(new Size2D((Game.RenderWidth / 2) - (Game.RenderWidth / 20), (Game.RenderHeight / 4) - (Game.RenderHeight / 20)));
            UpdateBackground();
        }
        public PartyGUIMember(SpritedBattlePokemon pkmn, SpriteList sprites)
        {
            _usePartyPkmn = false;
            _isEgg = pkmn.PartyPkmn.IsEgg;
            _battlePkmn = pkmn;
            _color = GetColor();
            _mini = new Sprite()
            {
                Image = pkmn.Mini,
                Pos = new Pos2D(0, Sprite_BounceDefY)
            };
            if (!_isEgg)
            {
                _mini.Callback = Sprite_Bounce;
                _mini.Data = new Sprite_BounceData();
            }
            sprites.Add(_mini);
            _background = new WriteableImage(new Size2D((Game.RenderWidth / 2) - (Game.RenderWidth / 20), (Game.RenderHeight / 4) - (Game.RenderHeight / 20)));
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
                s.Pos.Y += data.Speed;
                if (s.Pos.Y >= data.Target)
                {
                    s.Pos.Y = data.Target;
                    data.Down = false;
                    data.Target = Sprite_BounceMinY;
                }
            }
            else
            {
                s.Pos.Y -= data.Speed;
                if (s.Pos.Y <= data.Target)
                {
                    s.Pos.Y = data.Target;
                    data.Down = true;
                    data.Target = Sprite_BounceMaxY;
                }
            }
        }

        public void SetBounce(bool big)
        {
            // Don't bounce eggs or fainted mon
            if (_isEgg)
            {
                return;
            }
            int speed;
            if (_usePartyPkmn ? _partyPkmn.HP == 0 : _battlePkmn.Pkmn.HP == 0)
            {
                speed = 0;
            }
            else
            {
                speed = big ? 2 : 1;
            }
            ((Sprite_BounceData)_mini.Data).Speed = speed;
        }

        #endregion

        public void UpdateBackground()
        {
            GL gl = Game.OpenGL;
            _background.PushFrameBuffer(gl);

            GLHelper.ClearColor(gl, _color);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            // TODO: Shadow
            //Renderer.FillEllipse_Points(dst, dstW, dstH, 3, 34, 29, 39, Renderer.Color(0, 0, 0, 100));
            // Nickname
            PartyPokemon p = _usePartyPkmn ? _partyPkmn : _battlePkmn.PartyPkmn;
            GUIString.CreateAndRenderOneTimeString(gl, p.Nickname, Font.DefaultSmall, FontColors.DefaultWhite_I, new Pos2D(2, 3));
            if (_isEgg)
            {
                goto bottom;
            }
            PBEBattlePokemon bPkmn = _usePartyPkmn ? null : _battlePkmn.Pkmn;
            // Gender
            GUIString.CreateAndRenderOneTimeGenderString(gl, p.Gender, Font.Default, new Pos2D(61, -2));
            // Level
            const int lvX = 72;
            GUIString.CreateAndRenderOneTimeString(gl, "[LV]", Font.PartyNumbers, FontColors.DefaultWhite_I, new Pos2D(lvX, 3));
            GUIString.CreateAndRenderOneTimeString(gl, (_usePartyPkmn ? p.Level : bPkmn.Level).ToString(), Font.PartyNumbers, FontColors.DefaultWhite_I, new Pos2D(lvX + 12, 3));
            // Status
            PBEStatus1 status = _usePartyPkmn ? p.Status1 : bPkmn.Status1;
            if (status != PBEStatus1.None)
            {
                GUIString.CreateAndRenderOneTimeString(gl, status.ToString(), Font.DefaultSmall, FontColors.DefaultWhite_I, new Pos2D(61, 13));
            }
            // Item
            ItemType item = _usePartyPkmn ? p.Item : (ItemType)bPkmn.Item;
            if (item != ItemType.None)
            {
                GUIString.CreateAndRenderOneTimeString(gl, ItemData.GetItemName(item), Font.DefaultSmall, FontColors.DefaultWhite_I, new Pos2D(61, 23));
            }
        bottom:
            GLHelper.PopFrameBuffer(gl);
        }

        // We shouldn't be redrawing on the logic thread, but it currently doesn't matter
        public void UpdateColorAndRedraw()
        {
            _color = GetColor();
            UpdateBackground();
        }
        private ColorF GetColor()
        {
            if (_usePartyPkmn)
            {
                PartyPokemon pp = _partyPkmn;
                if (!_isEgg && pp.HP == 0)
                {
                    return GetFaintedColor();
                }
                return GetDefaultColor();
            }

            SpritedBattlePokemon s = _battlePkmn;
            if (!_isEgg && s.Pkmn.HP == 0)
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
        private static ColorF GetDefaultColor()
        {
            return ColorF.FromRGBA(48, 48, 48, 128);
        }
        private static ColorF GetFaintedColor()
        {
            return ColorF.FromRGBA(120, 30, 60, 196);
        }
        private static ColorF GetActiveColor()
        {
            return ColorF.FromRGBA(255, 192, 60, 96);
        }
        private static ColorF GetStandByColor()
        {
            return ColorF.FromRGBA(125, 255, 195, 100);
        }

        public void Render(Pos2D pos, bool selected)
        {
            _background.Render(pos);
            if (selected)
            {
                GUIRenderer.Instance.DrawRectangle(ColorF.FromRGBA(48, 180, 255, 200), new Rect2D(pos, _background.Size));
            }
            _mini.Render(pos);
        }

        public void Delete(GL gl)
        {
            if (_usePartyPkmn)
            {
                _mini.Image.DeductReference(gl);
            }
            _background.DeductReference(gl);
        }
    }
}
