using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed class PartyMenuGUI
    {
        private sealed unsafe class PartyMemberButton : GUIButton
        {
            public SpritedBattlePokemon SPkmn { get; }
            private readonly Sprite _drawn;

            public PartyMemberButton(float x, float y, SpritedBattlePokemon sPkmn)
            {
                X = x;
                Y = y;
                Width = 0.48f;
                Height = 0.31f;
                SPkmn = sPkmn;

                _drawn = new Sprite(_slot.Width, _slot.Height);
                _drawn.Draw(Draw);
            }

            private unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight)
            {
                _slot.DrawOn(bmpAddress, bmpWidth, bmpHeight, 0, 0);
                if (SPkmn is null) // Effectively the same as IsDisabled
                {
                    RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(49, 49, 49, 128)); // Looks bad because it also affects the transparent bits
                    return;
                }

                PBEBattlePokemon pkmn = SPkmn.Pkmn;
                Font fontDefault = Font.Default;
                Font fontPartyNumbers = Font.PartyNumbers;
                uint[] defaultWhite = Font.DefaultWhite;

                fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, 31, 6, pkmn.Nickname, defaultWhite);
                fontPartyNumbers.DrawString(bmpAddress, bmpWidth, bmpHeight, 7, 31, "[LV]", defaultWhite);
                fontPartyNumbers.DrawString(bmpAddress, bmpWidth, bmpHeight, 19, 31, pkmn.Level.ToString(), defaultWhite);
                string str = pkmn.HP.ToString();
                fontPartyNumbers.MeasureString(str, out int strW, out int _);
                fontPartyNumbers.DrawString(bmpAddress, bmpWidth, bmpHeight, 87 - strW, 31, str, defaultWhite);
                fontPartyNumbers.DrawString(bmpAddress, bmpWidth, bmpHeight, 88, 31, "/" + pkmn.MaxHP, defaultWhite);
                PBEGender gender = pkmn.Gender;
                if (gender != PBEGender.Genderless)
                {
                    fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, 113, 6, gender.ToSymbol(), gender == PBEGender.Male ? Font.DefaultMale : Font.DefaultFemale);
                }

                SPkmn.Minisprite.DrawOn(bmpAddress, bmpWidth, bmpHeight, 0 - 1, 0 - 1);

                // Draw HP
                const int lineStartX = 64;
                const int lineStartY = 27;
                const int lineW = 48;
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, lineStartX - 1, lineStartY - 1, lineW + 2, 4, RenderUtils.Color(49, 49, 49, 255));
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, lineStartX, lineStartY, lineW, 2, RenderUtils.Color(33, 33, 33, 255));
                double hpp = pkmn.HPPercentage;
                int theW = (int)(lineW * hpp);
                if (theW == 0 && hpp > 0)
                {
                    theW = 1;
                }
                RenderUtils.DrawHorizontalLine_Width(bmpAddress, bmpWidth, bmpHeight, lineStartX, lineStartY, theW, RenderUtils.Color(99, 255, 99, 255));
                RenderUtils.DrawHorizontalLine_Width(bmpAddress, bmpWidth, bmpHeight, lineStartX, lineStartY + 1, theW, RenderUtils.Color(24, 198, 33, 255));
                _hpText.DrawOn(bmpAddress, bmpWidth, bmpHeight, 47, 23);
            }

            public unsafe void Render(uint* bmpAddress, int bmpWidth, int bmpHeight)
            {
                int x = (int)(X * bmpWidth);
                int y = (int)(Y * bmpHeight);
                int width = (int)(Width * bmpWidth);
                int height = (int)(Height * bmpHeight);
                _drawn.DrawOn(bmpAddress, bmpWidth, bmpHeight, x, y, width, height);
            }
        }

        private static readonly Sprite _background = Sprite.LoadOrGet("GUI.PartyMenu.Background.png");
        private static readonly Sprite _slot = Sprite.LoadOrGet("GUI.PartyMenu.Slot.png");
        private static readonly Sprite _hpText = Sprite.LoadOrGet("GUI.PartyMenu.HP.png");

        private FadeFromColorTransition _fadeFromTransition;
        private FadeToColorTransition _fadeToTransition;

        private readonly SpritedBattlePokemonParty _party;
        private readonly GUIButtons<PartyMemberButton> _buttons;
        private Action _onClosed;

        public PartyMenuGUI(SpritedBattlePokemonParty party, Action onClosed)
        {
            _party = party;
            _buttons = new GUIButtons<PartyMemberButton>
            {
                new PartyMemberButton(0.01f, 0.01f, _party.SpritedParty.Length > 0 ? _party.SpritedParty[0] : null),
                new PartyMemberButton(0.51f, 0.01f, _party.SpritedParty.Length > 1 ? _party.SpritedParty[1] : null),
                new PartyMemberButton(0.01f, 0.33f, _party.SpritedParty.Length > 2 ? _party.SpritedParty[2] : null),
                new PartyMemberButton(0.51f, 0.33f, _party.SpritedParty.Length > 3 ? _party.SpritedParty[3] : null),
                new PartyMemberButton(0.01f, 0.65f, _party.SpritedParty.Length > 4 ? _party.SpritedParty[4] : null),
                new PartyMemberButton(0.51f, 0.65f, _party.SpritedParty.Length > 5 ? _party.SpritedParty[5] : null)
            };
            _onClosed = onClosed;
            void FadeFromTransitionEnded()
            {
                _fadeFromTransition = null;
            }
            _fadeFromTransition = new FadeFromColorTransition(20, 0, FadeFromTransitionEnded);
        }

        public void LogicTick()
        {
            if (_fadeToTransition != null || _fadeFromTransition != null)
            {
                return;
            }

            bool b = InputManager.IsPressed(Key.B);
            if (!b)
            {
                return;
            }

            if (b)
            {
                void FadeToTransitionEnded()
                {
                    _fadeToTransition = null;
                    _onClosed.Invoke();
                    _onClosed = null;
                }
                _fadeToTransition = new FadeToColorTransition(20, 0, FadeToTransitionEnded);
            }
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            _background.DrawOn(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight);

            foreach (PartyMemberButton button in _buttons)
            {
                button.Render(bmpAddress, bmpWidth, bmpHeight);
            }

            _fadeFromTransition?.RenderTick(bmpAddress, bmpWidth, bmpHeight);
            _fadeToTransition?.RenderTick(bmpAddress, bmpWidth, bmpHeight);
        }
    }
}
