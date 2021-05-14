using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    internal sealed class PartyGUIMember
    {
        private readonly bool _usePartyPkmn;
        private readonly uint _color;
        private readonly PartyPokemon _partyPkmn;
        private readonly SpritedBattlePokemon _battlePkmn;
        private readonly Sprite _mini;
        private readonly Image _background;

        public PartyGUIMember(PartyPokemon pkmn, List<Sprite> sprites)
        {
            _usePartyPkmn = true;
            if (!pkmn.IsEgg && pkmn.HP == 0)
            {
                _color = GetFaintedColor();
            }
            else
            {
                _color = GetDefaultColor();
            }
            _partyPkmn = pkmn;
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
        public PartyGUIMember(SpritedBattlePokemon pkmn, List<Sprite> sprites)
        {
            _usePartyPkmn = false;
            if (pkmn.Pkmn.FieldPosition != PBEFieldPosition.None)
            {
                _color = GetActiveColor();
            }
            else if (BattleGUI.Instance.StandBy.Contains(pkmn.Pkmn))
            {
                _color = GetStandByColor();
            }
            else if (!pkmn.PartyPkmn.IsEgg && pkmn.Pkmn.HP == 0)
            {
                _color = GetFaintedColor();
            }
            else
            {
                _color = GetDefaultColor();
            }
            _battlePkmn = pkmn;
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
        private unsafe void DrawBackground(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.OverwriteRectangle(bmpAddress, bmpWidth, bmpHeight, _color);
            // Shadow
            RenderUtils.FillEllipse_Points(bmpAddress, bmpWidth, bmpHeight, 3, 34, 29, 39, RenderUtils.Color(0, 0, 0, 100));
            // Nickname
            PartyPokemon p = _usePartyPkmn ? _partyPkmn : _battlePkmn.PartyPkmn;
            Font.DefaultSmall.DrawString(bmpAddress, bmpWidth, bmpHeight, 2, 3, p.Nickname, Font.DefaultWhite);
            if (p.IsEgg)
            {
                return;
            }
            PBEBattlePokemon bPkmn = _usePartyPkmn ? null : _battlePkmn.Pkmn;
            // Gender
            PBEGender gender = p.Gender;
            if (gender != PBEGender.Genderless)
            {
                Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, 61, -2, gender.ToSymbol(), gender == PBEGender.Male ? Font.DefaultMale : Font.DefaultFemale);
            }
            // Level
            const int lvX = 72;
            Font.PartyNumbers.DrawString(bmpAddress, bmpWidth, bmpHeight, lvX, 3, "[LV]", Font.DefaultWhite);
            Font.PartyNumbers.DrawString(bmpAddress, bmpWidth, bmpHeight, lvX + 12, 3, (_usePartyPkmn ? p.Level : bPkmn.Level).ToString(), Font.DefaultWhite);
            // Status
            PBEStatus1 status = _usePartyPkmn ? p.Status1 : bPkmn.Status1;
            if (status != PBEStatus1.None)
            {
                Font.DefaultSmall.DrawString(bmpAddress, bmpWidth, bmpHeight, 61, 13, status.ToString(), Font.DefaultWhite);
            }
            // Item
            ItemType item = _usePartyPkmn ? p.Item : (ItemType)bPkmn.Item;
            if (item != ItemType.None)
            {
                Font.DefaultSmall.DrawString(bmpAddress, bmpWidth, bmpHeight, 61, 23, ItemData.GetItemName(item), Font.DefaultWhite);
            }
        }
        private uint GetDefaultColor()
        {
            return RenderUtils.Color(48, 48, 48, 128);
        }
        private uint GetFaintedColor()
        {
            return RenderUtils.Color(120, 30, 60, 196);
        }
        private uint GetActiveColor()
        {
            return RenderUtils.Color(255, 192, 60, 96);
        }
        private uint GetStandByColor()
        {
            return RenderUtils.Color(125, 255, 195, 100);
        }

        public unsafe void Render(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, bool selected)
        {
            _background.DrawOn(bmpAddress, bmpWidth, bmpHeight, x, y);
            if (selected)
            {
                RenderUtils.DrawRectangle(bmpAddress, bmpWidth, bmpHeight, x, y, _background.Width, _background.Height, RenderUtils.Color(48, 180, 255, 200));
            }
            _mini.DrawOn(bmpAddress, bmpWidth, bmpHeight, xOffset: x, yOffset: y);
        }
    }
}
