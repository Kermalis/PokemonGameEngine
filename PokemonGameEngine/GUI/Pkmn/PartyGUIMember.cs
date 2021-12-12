using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    internal sealed class PartyGUIMember
    {
        private readonly bool _usePartyPkmn;
        private readonly bool _isEgg;
        private Vector4 _color;
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
                Image = PokemonImageLoader.GetMini(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny, pkmn.IsEgg)
            };
            if (!_isEgg)
            {
                _mini.Callback = Sprite_Bounce;
                _mini.Data = new Sprite_BounceData();
            }
            sprites.Add(_mini);
            _background = new WriteableImage(new Size2D((Display.RenderWidth / 2) - (Display.RenderWidth / 20), (Display.RenderHeight / 4) - (Display.RenderHeight / 20)));
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
                Image = pkmn.Mini
            };
            if (!_isEgg)
            {
                _mini.Callback = Sprite_Bounce;
                _mini.Data = new Sprite_BounceData();
            }
            sprites.Add(_mini);
            _background = new WriteableImage(new Size2D((Display.RenderWidth / 2) - (Display.RenderWidth / 20), (Display.RenderHeight / 4) - (Display.RenderHeight / 20)));
            UpdateBackground();
        }

        #region Sprite Callbacks

        private const int SPRITE_BOUNCE_MID_Y = 7;
        private const float SPRITE_BOUNCE_SPEED_BIG = 6f;
        private const float SPRITE_BOUNCE_SPEED_SMALL = 4f;
        private class Sprite_BounceData { public float Speed = SPRITE_BOUNCE_SPEED_SMALL; public float Counter; }
        private static void Sprite_Bounce(Sprite s)
        {
            var data = (Sprite_BounceData)s.Data;
            data.Counter += Display.DeltaTime * data.Speed;
            float f = MathF.Cos(data.Counter * MathF.PI * 2f);
            int y;
            if (f >= 0.5f)
            {
                y = +1;
            }
            else if (f <= -0.5f)
            {
                y = -1;
            }
            else
            {
                y = 0;
            }
            s.Pos.Y = SPRITE_BOUNCE_MID_Y + y;
        }

        public void SetBounce(bool big)
        {
            if (_isEgg)
            {
                return; // Don't bounce eggs or fainted mon
            }

            float speed;
            if (_usePartyPkmn ? _partyPkmn.HP == 0 : _battlePkmn.Pkmn.HP == 0)
            {
                speed = 0f; // Pause bounce for fainted
            }
            else
            {
                speed = big ? SPRITE_BOUNCE_SPEED_BIG : SPRITE_BOUNCE_SPEED_SMALL;
            }
            var data = (Sprite_BounceData)_mini.Data;
            data.Speed = speed;
        }

        #endregion

        public void UpdateBackground()
        {
            GL gl = Display.OpenGL;
            _background.PushFrameBuffer(gl);
            GLHelper.ClearColor(gl, _color);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            // TODO: Shadow
            //Renderer.FillEllipse_Points(dst, dstW, dstH, 3, 34, 29, 39, Renderer.Color(0, 0, 0, 100));

            // Nickname
            PartyPokemon p = _usePartyPkmn ? _partyPkmn : _battlePkmn.PartyPkmn;
            GUIString.CreateAndRenderOneTimeString(p.Nickname, Font.DefaultSmall, FontColors.DefaultWhite_I, new Pos2D(2, 3));
            if (_isEgg)
            {
                goto bottom;
            }

            PBEBattlePokemon bPkmn = _usePartyPkmn ? null : _battlePkmn.Pkmn;
            // Gender
            GUIString.CreateAndRenderOneTimeGenderString(p.Gender, Font.Default, new Pos2D(61, -2));
            // Level
            const int lvX = 72;
            GUIString.CreateAndRenderOneTimeString("[LV]", Font.PartyNumbers, FontColors.DefaultWhite_I, new Pos2D(lvX, 3));
            GUIString.CreateAndRenderOneTimeString((_usePartyPkmn ? p.Level : bPkmn.Level).ToString(), Font.PartyNumbers, FontColors.DefaultWhite_I, new Pos2D(lvX + 12, 3));
            // Status
            PBEStatus1 status = _usePartyPkmn ? p.Status1 : bPkmn.Status1;
            if (status != PBEStatus1.None)
            {
                GUIString.CreateAndRenderOneTimeString(status.ToString(), Font.DefaultSmall, FontColors.DefaultWhite_I, new Pos2D(61, 13));
            }
            // Item
            ItemType item = _usePartyPkmn ? p.Item : (ItemType)bPkmn.Item;
            if (item != ItemType.None)
            {
                GUIString.CreateAndRenderOneTimeString(ItemData.GetItemName(item), Font.DefaultSmall, FontColors.DefaultWhite_I, new Pos2D(61, 23));
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
        private Vector4 GetColor()
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
        private static Vector4 GetDefaultColor()
        {
            return Colors.FromRGBA(48, 48, 48, 128);
        }
        private static Vector4 GetFaintedColor()
        {
            return Colors.FromRGBA(120, 30, 60, 196);
        }
        private static Vector4 GetActiveColor()
        {
            return Colors.FromRGBA(255, 192, 60, 96);
        }
        private static Vector4 GetStandByColor()
        {
            return Colors.FromRGBA(125, 255, 195, 100);
        }

        public void Render(Pos2D pos, bool selected)
        {
            _background.Render(pos);
            if (selected)
            {
                GUIRenderer.Instance.DrawRectangle(Colors.FromRGBA(48, 180, 255, 200), new Rect2D(pos, _background.Size));
            }
            _mini.Render(pos);
        }

        public void Delete()
        {
            if (_usePartyPkmn)
            {
                _mini.Image.DeductReference();
            }
            _background.DeductReference();
        }
    }
}
