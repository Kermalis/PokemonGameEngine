using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.UI;
using Kermalis.PokemonGameEngine.Util;
using Kermalis.PokemonGameEngine.World;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    // TODO: Eggs
    // TODO: Different modes (like select a move)
    // TODO: Box mon, battle mon
    // TODO: Up/down
    internal sealed class SummaryGUI
    {
        public enum Mode : byte
        {
            JustView
        }
        private enum Page : byte
        {
            Info,
            Personal,
            Stats,
            Moves,
            Ribbons
        }

        private readonly Mode _mode;
        private Page _page;
        private readonly PartyPokemon _currentPkmn;

        private FadeColorTransition _fadeTransition;
        private Action _onClosed;

        private AnimatedImage _pkmnImage;
        private readonly Image _pageImage;
        private const float PageImageWidth = 0.55f;
        private const float PageImageHeight = 0.9f;

        #region Open & Close GUI

        public unsafe SummaryGUI(PartyPokemon pkmn, Mode mode, Action onClosed)
        {
            _mode = mode;
            _page = Page.Info;

            _pageImage = new Image((int)(Program.RenderWidth * PageImageWidth), (int)(Program.RenderHeight * PageImageHeight));

            _currentPkmn = pkmn;
            LoadPkmnImage();
            UpdatePageImage();

            _onClosed = onClosed;
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeInSummary);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void CloseSummaryMenu()
        {
            _fadeTransition = new FadeToColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeOutSummary);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void CB_FadeInSummary()
        {
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Game.Instance.SetCallback(CB_InfoPage);
                Game.Instance.SetRCallback(RCB_RenderTick);
            }
        }
        private unsafe void CB_FadeOutSummary()
        {
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _onClosed.Invoke();
                _onClosed = null;
            }
        }

        private unsafe void RCB_Fading(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RCB_RenderTick(bmpAddress, bmpWidth, bmpHeight);
            _fadeTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
        }

        #endregion

        private void LoadPkmnImage()
        {
            PBESpecies species = _currentPkmn.Species;
            PBEForm form = _currentPkmn.Form;
            PBEGender gender = _currentPkmn.Gender;
            bool shiny = _currentPkmn.Shiny;
            uint pid = _currentPkmn.PID;
            bool egg = _currentPkmn.IsEgg;
            _pkmnImage = PokemonImageUtils.GetPokemonImage(species, form, gender, shiny, false, false, pid, egg);
        }
        private void SwapPage(Page newPage)
        {
            _page = newPage;
            UpdatePageImage();
            Game.MainCallback cb;
            switch (newPage)
            {
                case Page.Info: cb = CB_InfoPage; break;
                case Page.Personal: cb = CB_PersonalPage; break;
                default: throw new Exception();
            }
            Game.Instance.SetCallback(cb);
        }

        private unsafe void UpdatePageImage()
        {
            _pageImage.Draw(DrawPage);
        }
        private unsafe void DrawPage(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.OverwriteRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(0, 0, 0, 0));
            Font.Default.DrawStringScaled(bmpAddress, bmpWidth, bmpHeight, 0, 0, 2, _page.ToString(), Font.DefaultBlack_I);

            switch (_page)
            {
                case Page.Info: DrawInfoPage(bmpAddress, bmpWidth, bmpHeight); break;
                case Page.Personal: DrawPersonalPage(bmpAddress, bmpWidth, bmpHeight); break;
            }
        }
        private unsafe void DrawInfoPage(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            const float winX = 0.03f;
            const float winY = 0.15f;
            const float leftColX = winX + 0.02f;
            const float textStartY = winY + 0.05f;
            const float textSpacingY = 0.1f;
            const float rightColX = winX + 0.52f;
            const float rightColY = winY + 0.03f;
            const float rightColW = 0.4f;
            const float rightColH = 0.62f;
            const float winW = rightColX + rightColW + 0.02f - winX;
            const float winH = rightColY + rightColH + 0.03f - winY;
            const float rightColCenterX = rightColX + (rightColW / 2f);
            RenderUtils.FillRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, winX, winY, winX + winW, winY + winH, 15, RenderUtils.Color(128, 215, 135, 255));
            RenderUtils.FillRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, rightColX, rightColY, rightColX + rightColW, rightColY + rightColH, 10, RenderUtils.Color(210, 210, 210, 255));

            Font leftColFont = Font.Default;
            uint[] leftColColors = Font.DefaultWhite2_I;
            Font rightColFont = Font.Default;
            uint[] rightColColors = Font.DefaultBlack_I;

            void PlaceLeftCol(int i, string leftColStr)
            {
                float y = textStartY + (i * textSpacingY);
                leftColFont.DrawString(bmpAddress, bmpWidth, bmpHeight, leftColX, y, leftColStr, leftColColors);
            }
            void PlaceRightCol(int i, string rightColStr, uint[] colors)
            {
                float y = textStartY + (i * textSpacingY);
                rightColFont.MeasureString(rightColStr, out int strW, out _);
                rightColFont.DrawString(bmpAddress, bmpWidth, bmpHeight,
                    RenderUtils.GetCoordinatesForCentering(bmpWidth, strW, rightColCenterX), (int)(bmpHeight * y), rightColStr, colors);
            }

            PlaceLeftCol(0, "Species");
            PlaceLeftCol(1, "Type(s)");
            PlaceLeftCol(2, "OT");
            PlaceLeftCol(3, "OT ID");
            PlaceLeftCol(4, "Exp. Points");
            PlaceLeftCol(5, "Exp. To Next Level");

            PBESpecies species = _currentPkmn.Species;
            PBEForm form = _currentPkmn.Form;
            var bs = new BaseStats(species, form);
            OTInfo ot = _currentPkmn.OT;
            uint exp = _currentPkmn.EXP;
            uint toNextLvl = _currentPkmn.Level >= PkmnConstants.MaxLevel ? 0 : PBEEXPTables.GetEXPRequired(bs.GrowthRate, (byte)(_currentPkmn.Level + 1)) - exp;

            // Species
            string str = PBELocalizedString.GetSpeciesName(species).English;
            PlaceRightCol(0, str, rightColColors);
            // Types
            str = PBELocalizedString.GetTypeName(bs.Type1).English;
            if (bs.Type2 != PBEType.None)
            {
                str += ' ' + PBELocalizedString.GetTypeName(bs.Type2).English;
            }
            PlaceRightCol(1, str, rightColColors);
            // OT
            str = ot.TrainerName;
            PlaceRightCol(2, str, ot.TrainerIsFemale ? Font.DefaultRed_I : Font.DefaultBlue_I);
            // OT ID
            str = ot.TrainerID.ToString();
            PlaceRightCol(3, str, rightColColors);
            // Exp
            str = exp.ToString();
            PlaceRightCol(4, str, rightColColors);
            // To next level
            str = toNextLvl.ToString();
            PlaceRightCol(5, str, rightColColors);
        }
        private unsafe void DrawPersonalPage(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            const float winX = 0.08f;
            const float winY = 0.15f;
            const float leftColX = winX + 0.03f;
            const float textStartY = winY + 0.05f;
            const float textSpacingY = 0.1f;
            const float winW = 0.75f - winX;
            const float winH = 0.93f - winY;
            RenderUtils.FillRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, winX, winY, winX + winW, winY + winH, 15, RenderUtils.Color(145, 225, 225, 255));

            Font leftColFont = Font.Default;
            uint[] leftColColors = Font.DefaultBlack_I;
            uint[] highlightColors = Font.DefaultRed_I;

            void Place(int i, int xOff, string leftColStr, uint[] colors)
            {
                float y = textStartY + (i * textSpacingY);
                leftColFont.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * leftColX) + xOff, (int)(bmpHeight * y), leftColStr, colors);
            }

            PBENature nature = _currentPkmn.Nature;
            DateTime met = _currentPkmn.MetDate;
            MapSection loc = _currentPkmn.MetLocation;
            byte metLvl = _currentPkmn.MetLevel;
            string characteristic = Characteristic.GetCharacteristic(_currentPkmn.PID, _currentPkmn.IndividualValues) + '.';
            PBEFlavor? flavor = PBEDataUtils.GetLikedFlavor(nature);

            // Nature
            string str = PBELocalizedString.GetNatureName(nature).English + ' ';
            Place(0, 0, str, highlightColors);
            leftColFont.MeasureString(str, out int strW, out _);
            str = "nature.";
            Place(0, strW, str, leftColColors);
            // Met date
            str = met.ToString("MMMM dd, yyyy");
            Place(1, 0, str, leftColColors);
            // Met location
            str = loc.ToString();
            Place(2, 0, str, highlightColors);
            // Met level
            str = string.Format("Met at Level {0}.", metLvl);
            Place(3, 0, str, leftColColors);
            // Characteristic
            str = characteristic;
            Place(5, 0, str, leftColColors);
            // Flavor
            if (flavor.HasValue)
            {
                str = "Likes ";
                Place(6, 0, str, leftColColors);
                leftColFont.MeasureString(str, out strW, out _);
                str = flavor.Value.ToString() + ' ';
                Place(6, strW, str, highlightColors);
                leftColFont.MeasureString(str, out int strW2, out _);
                str = "food.";
                Place(6, strW + strW2, str, leftColColors);
            }
            else
            {
                str = "Likes all food.";
                Place(6, 0, str, leftColColors);
            }
        }

        private void CB_InfoPage()
        {
            if (InputManager.IsPressed(Key.B))
            {
                CloseSummaryMenu();
                return;
            }
            if (InputManager.IsPressed(Key.Right))
            {
                SwapPage(Page.Personal);
                return;
            }
        }
        private void CB_PersonalPage()
        {
            if (InputManager.IsPressed(Key.B))
            {
                CloseSummaryMenu();
                return;
            }
            if (InputManager.IsPressed(Key.Left))
            {
                SwapPage(Page.Info);
                return;
            }
        }

        private unsafe void RCB_RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.ThreeColorBackground(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(215, 231, 230, 255), RenderUtils.Color(231, 163, 0, 255), RenderUtils.Color(242, 182, 32, 255));

            AnimatedImage.UpdateCurrentFrameForAll();
            _pkmnImage.DrawOn(bmpAddress, bmpWidth, bmpHeight,
                RenderUtils.GetCoordinatesForCentering(bmpWidth, _pkmnImage.Width, 0.2f), RenderUtils.GetCoordinatesForEndAlign(bmpHeight, _pkmnImage.Height, 0.6f));

            _pageImage.DrawOn(bmpAddress, bmpWidth, bmpHeight, 1f - PageImageWidth, 1f - PageImageHeight);
        }
    }
}
