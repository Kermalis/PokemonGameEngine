using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.Battle;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Pkmn
{
    internal sealed class PartyGUIMember
    {
        private readonly Rect _rect;

        private readonly bool _usePartyPkmn;
        private readonly bool _isEgg;
        private Vector4 _color;
        private readonly PartyPokemon _partyPkmn;
        private readonly BattlePokemon _battlePkmn;
        private readonly Sprite _mini;
        private readonly FrameBuffer _frameBuffer;

        private PartyGUIMember(bool isEgg, Rect rect, ConnectedList<Sprite> sprites)
        {
            _rect = rect;
            _mini = new Sprite();
            sprites.Add(_mini);
            _isEgg = isEgg;
            if (!isEgg)
            {
                _mini.Callback = Sprite_Bounce;
                _mini.Data = new Sprite_BounceData();
            }
            _frameBuffer = new FrameBuffer().AddColorTexture(rect.GetSize());
        }
        public PartyGUIMember(PartyPokemon pkmn, Rect rect, ConnectedList<Sprite> sprites, bool isSelected)
            : this(pkmn.IsEgg, rect, sprites)
        {
            _usePartyPkmn = true;
            _partyPkmn = pkmn;
            _color = GetColor();
            if (_isEgg)
            {
                _mini.Image = PokemonImageLoader.GetEggMini();
            }
            else
            {
                _mini.Image = PokemonImageLoader.GetMini(pkmn.Species, pkmn.Form, pkmn.Gender, pkmn.Shiny);
                SetBounce(isSelected);
            }
            UpdateBackground();
        }
        public PartyGUIMember(BattlePokemon pkmn, Rect rect, ConnectedList<Sprite> sprites, bool isSelected)
            : this(pkmn.PartyPkmn.IsEgg, rect, sprites)
        {
            _usePartyPkmn = false;
            _battlePkmn = pkmn;
            _color = GetColor();
            _mini.Image = pkmn.Mini;
            SetBounce(isSelected);
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
                return; // Don't bounce eggs
            }

            var data = (Sprite_BounceData)_mini.Data;
            if (_usePartyPkmn ? _partyPkmn.HP == 0 : _battlePkmn.PBEPkmn.HP == 0)
            {
                data.Speed = 0f; // Pause bounce for fainted
            }
            else
            {
                data.Speed = big ? SPRITE_BOUNCE_SPEED_BIG : SPRITE_BOUNCE_SPEED_SMALL;
            }
        }

        #endregion

        public void UpdateBackground()
        {
            GL gl = Display.OpenGL;
            _frameBuffer.UseAndViewport(gl);
            gl.ClearColor(Colors.Transparent);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            GUIRenderer.Rect(_color, Rect.FromSize(new Vec2I(0, 0), _frameBuffer.ColorTextures[0].Size), cornerRadii: new(6));

            // Shadow
            GUIRenderer.Rect(Colors.FromRGBA(0, 0, 0, 200), Rect.FromCorners(new Vec2I(3, 34), new Vec2I(29, 39)), cornerRadii: new(10));

            // Nickname
            PartyPokemon p = _usePartyPkmn ? _partyPkmn : _battlePkmn.PartyPkmn;
            GUIString.CreateAndRenderOneTimeString(p.Nickname, Font.DefaultSmall, FontColors.DefaultWhite_I, new Vec2I(2, 3));
            if (_isEgg)
            {
                return;
            }

            PBEBattlePokemon bPkmn = _usePartyPkmn ? null : _battlePkmn.PBEPkmn;
            // Gender
            GUIString.CreateAndRenderOneTimeGenderString(p.Gender, Font.Default, new Vec2I(61, -2));
            // Level
            const int lvX = 72;
            GUIString.CreateAndRenderOneTimeString("[LV]", Font.PartyNumbers, FontColors.DefaultWhite_I, new Vec2I(lvX, 3));
            GUIString.CreateAndRenderOneTimeString((_usePartyPkmn ? p.Level : bPkmn.Level).ToString(), Font.PartyNumbers, FontColors.DefaultWhite_I, new Vec2I(lvX + 12, 3));
            // Status
            PBEStatus1 status = _usePartyPkmn ? p.Status1 : bPkmn.Status1;
            if (status != PBEStatus1.None)
            {
                GUIString.CreateAndRenderOneTimeString(status.ToString(), Font.DefaultSmall, FontColors.DefaultWhite_I, new Vec2I(61, 13));
            }
            // Item
            ItemType item = _usePartyPkmn ? p.Item : (ItemType)bPkmn.Item;
            if (item != ItemType.None)
            {
                GUIString.CreateAndRenderOneTimeString(ItemData.GetItemName(item), Font.DefaultSmall, FontColors.DefaultWhite_I, new Vec2I(61, 23));
            }
        }

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

            BattlePokemon s = _battlePkmn;
            if (!_isEgg && s.PBEPkmn.HP == 0)
            {
                return GetFaintedColor();
            }
            if (s.PBEPkmn.FieldPosition != PBEFieldPosition.None)
            {
                return GetActiveColor();
            }
            ActionsBuilder a = BattleGUI.Instance.ActionsBuilder;
            if (a is not null && a.IsStandBy(s.PBEPkmn))
            {
                return GetStandByColor();
            }
            SwitchesBuilder sb = BattleGUI.Instance.SwitchesBuilder;
            if (sb is not null && sb.IsStandBy(s.PBEPkmn))
            {
                return GetStandByColor();
            }
            return GetDefaultColor();
        }
        private static Vector4 GetDefaultColor()
        {
            return Colors.FromRGBA(48, 48, 48, 196);
        }
        private static Vector4 GetFaintedColor()
        {
            return Colors.FromRGBA(120, 30, 60, 224);
        }
        private static Vector4 GetActiveColor()
        {
            return Colors.FromRGBA(255, 192, 60, 128);
        }
        private static Vector4 GetStandByColor()
        {
            return Colors.FromRGBA(125, 255, 195, 128);
        }

        public void Render(bool isSelected)
        {
            _frameBuffer.RenderColorTexture(_rect.TopLeft);
            if (isSelected)
            {
                GUIRenderer.Rect(Colors.FromRGBA(48, 180, 255, 200), _rect, lineThickness: 1, cornerRadii: new(6));
            }
            _mini.Render(_rect.TopLeft);
        }

        public void Delete()
        {
            _frameBuffer.Delete();
            if (_usePartyPkmn)
            {
                _mini.Image.DeductReference();
            }
        }
    }
}
