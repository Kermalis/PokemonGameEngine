using Avalonia.Media.Imaging;
using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Util;
using System;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class PartyMenuGUI
    {
        private static readonly uint[][] _background = RenderUtil.LoadSprite("GUI.PartyMenu.Background.png");
        private static readonly uint[][] _slot = RenderUtil.LoadSprite("GUI.PartyMenu.Slot.png");
        private static readonly uint[][] _hpText = RenderUtil.LoadSprite("GUI.PartyMenu.HP.png");

        private FadeFromColorTransition _fadeFromTransition;
        private FadeToColorTransition _fadeToTransition;

        private readonly uint[][] _test = RenderUtil.ToColors(RenderUtil.ToWriteableBitmap(Utils.GetMinispriteBitmap(PBESpecies.Skitty, 0, PBEGender.Female, false)));

        private readonly PBEList<PBEBattlePokemon> _party;
        private Action _onClosed;

        public PartyMenuGUI(PBEList<PBEBattlePokemon> party, Action onClosed)
        {
            _party = party;
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

        private unsafe void RenderSlot(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, PBEBattlePokemon pkmn)
        {
            RenderUtil.DrawImage(bmpAddress, bmpWidth, bmpHeight, x, y, _slot, false, false);
            if (pkmn is null)
            {
                return;
            }

            Font fontDefault = Font.Default;
            Font fontPartyNumbers = Font.PartyNumbers;
            uint[] defaultWhite = Font.DefaultWhite;

            RenderUtil.DrawString(bmpAddress, bmpWidth, bmpHeight, x + 31, y + 6, pkmn.Nickname, fontDefault, defaultWhite);
            RenderUtil.DrawString(bmpAddress, bmpWidth, bmpHeight, x + 7, y + 31, "[LV]", fontPartyNumbers, defaultWhite);
            RenderUtil.DrawString(bmpAddress, bmpWidth, bmpHeight, x + 19, y + 31, pkmn.Level.ToString(), fontPartyNumbers, defaultWhite);
            string str = pkmn.HP.ToString();
            fontPartyNumbers.MeasureString(str, out int strW, out int _);
            RenderUtil.DrawString(bmpAddress, bmpWidth, bmpHeight, x + 87 - strW, y + 31, str, fontPartyNumbers, defaultWhite);
            RenderUtil.DrawString(bmpAddress, bmpWidth, bmpHeight, x + 88, y + 31, "/" + pkmn.MaxHP, fontPartyNumbers, defaultWhite);
            PBEGender gender = pkmn.Gender;
            if (gender != PBEGender.Genderless)
            {
                RenderUtil.DrawString(bmpAddress, bmpWidth, bmpHeight, x + 113, y + 6, gender.ToSymbol(), fontDefault, gender == PBEGender.Male ? Font.DefaultMale : Font.DefaultFemale);
            }

            RenderUtil.DrawImage(bmpAddress, bmpWidth, bmpHeight, x - 1, y - 1, _test, false, false);

            // Draw HP
            const int lineStartX = 64;
            const int lineStartY = 27;
            const int lineW = 48;
            RenderUtil.FillColor(bmpAddress, bmpWidth, bmpHeight, lineStartX - 1, lineStartY - 1, lineW + 2, 4, 0xFF313131);
            RenderUtil.FillColor(bmpAddress, bmpWidth, bmpHeight, lineStartX, lineStartY, lineW, 2, 0xFF212121);
            double hpp = pkmn.HPPercentage;
            int theW = (int)(lineW * hpp);
            if (theW == 0 && hpp > 0)
            {
                theW = 1;
            }
            RenderUtil.DrawLine(bmpAddress, bmpWidth, bmpHeight, lineStartX, lineStartY, lineStartX + theW, lineStartY, 0xFF63FF63);
            RenderUtil.DrawLine(bmpAddress, bmpWidth, bmpHeight, lineStartX, lineStartY + 1, lineStartX + theW, lineStartY + 1, 0xFF18C621);
            RenderUtil.DrawImage(bmpAddress, bmpWidth, bmpHeight, x + 47, y + 23, _hpText, false, false);
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtil.DrawStretchedImage(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight, _background);

            RenderSlot(bmpAddress, bmpWidth, bmpHeight, 1, 1, _party[0]);
            RenderSlot(bmpAddress, bmpWidth, bmpHeight, 129, 9, null);
            RenderSlot(bmpAddress, bmpWidth, bmpHeight, 1, 49, null);
            RenderSlot(bmpAddress, bmpWidth, bmpHeight, 129, 57, null);
            RenderSlot(bmpAddress, bmpWidth, bmpHeight, 1, 97, null);
            RenderSlot(bmpAddress, bmpWidth, bmpHeight, 129, 105, null);

            _fadeFromTransition?.RenderTick(bmpAddress, bmpWidth, bmpHeight);
            _fadeToTransition?.RenderTick(bmpAddress, bmpWidth, bmpHeight);
        }
    }
}
