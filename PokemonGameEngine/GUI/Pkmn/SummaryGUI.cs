using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.UI;
using Kermalis.PokemonGameEngine.Util;
using Kermalis.PokemonGameEngine.World;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    // TODO: Eggs
    // TODO: Up/down
    // TODO: Messages and confirm for learning moves
    // TODO: Move descriptions & stats/new move PP
    internal sealed class SummaryGUI
    {
        public enum Mode : byte
        {
            JustView,
            LearnMove
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
        private readonly BoxPokemon _pcPkmn;
        private readonly PartyPokemon _pPkmn;
        private readonly SpritedBattlePokemon _bPkmn;
        private readonly PBEMove _learningMove;
        private int _selectingMove = -1;

        private FadeColorTransition _fadeTransition;
        private Action _onClosed;

        private AnimatedImage _pkmnImage;
        private readonly Image _pageImage;
        private const float PageImageWidth = 0.55f;
        private const float PageImageHeight = 0.95f;

        #region Open & Close GUI

        public unsafe SummaryGUI(object pkmn, Mode mode, Action onClosed, PBEMove learningMove = PBEMove.None)
        {
            _mode = mode;
            if (mode == Mode.LearnMove)
            {
                SetSelectionVar(-1);
                _page = Page.Moves;
                _selectingMove = 0;
                _learningMove = learningMove;
            }
            else
            {
                _page = Page.Info;
            }

            _pageImage = new Image((int)(Program.RenderWidth * PageImageWidth), (int)(Program.RenderHeight * PageImageHeight));

            if (pkmn is PartyPokemon pPkmn)
            {
                _pPkmn = pPkmn;
            }
            else if (pkmn is BoxPokemon pcPkmn)
            {
                _pcPkmn = pcPkmn;
            }
            else
            {
                _bPkmn = (SpritedBattlePokemon)pkmn;
            }
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
                PlayPkmnCry();
                SetProperCallback();
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
            PBESpecies species;
            PBEForm form;
            PBEGender gender;
            bool shiny;
            uint pid;
            bool egg;
            if (_pPkmn is not null)
            {
                species = _pPkmn.Species;
                form = _pPkmn.Form;
                gender = _pPkmn.Gender;
                shiny = _pPkmn.Shiny;
                pid = _pPkmn.PID;
                egg = _pPkmn.IsEgg;
            }
            else if (_pcPkmn is not null)
            {
                species = _pcPkmn.Species;
                form = _pcPkmn.Form;
                gender = _pcPkmn.Gender;
                shiny = _pcPkmn.Shiny;
                pid = _pcPkmn.PID;
                egg = _pcPkmn.IsEgg;
            }
            else
            {
                PartyPokemon pPkmn = _bPkmn.PartyPkmn;
                PBEBattlePokemon bPkmn = _bPkmn.Pkmn;
                species = pPkmn.Species;
                form = bPkmn.RevertForm;
                gender = pPkmn.Gender;
                shiny = pPkmn.Shiny;
                pid = pPkmn.PID;
                egg = pPkmn.IsEgg;
            }
            _pkmnImage = PokemonImageUtils.GetPokemonImage(species, form, gender, shiny, false, false, pid, egg);
        }
        private void PlayPkmnCry()
        {
            PBESpecies species;
            PBEForm form;
            double hpPercentage;
            if (_pPkmn is not null)
            {
                if (_pPkmn.IsEgg)
                {
                    return;
                }
                species = _pPkmn.Species;
                form = _pPkmn.Form;
                hpPercentage = _pPkmn.HP / _pPkmn.MaxHP;
            }
            else if (_pcPkmn is not null)
            {
                if (_pcPkmn.IsEgg)
                {
                    return;
                }
                species = _pcPkmn.Species;
                form = _pcPkmn.Form;
                hpPercentage = 1;
            }
            else
            {
                PartyPokemon pPkmn = _bPkmn.PartyPkmn;
                if (pPkmn.IsEgg)
                {
                    return;
                }
                PBEBattlePokemon bPkmn = _bPkmn.Pkmn;
                species = pPkmn.Species;
                form = bPkmn.RevertForm;
                hpPercentage = bPkmn.HPPercentage;
            }
            SoundControl.PlayCry(species, form, hpPercentage);
        }
        private void SetProperCallback()
        {
            MainCallback cb;
            switch (_page)
            {
                case Page.Info: cb = CB_InfoPage; break;
                case Page.Personal: cb = CB_PersonalPage; break;
                case Page.Stats: cb = CB_StatsPage; break;
                case Page.Moves: cb = CB_MovesPage; break;
                default: throw new Exception();
            }
            Game.Instance.SetCallback(cb);
        }
        private void SwapPage(Page newPage)
        {
            _page = newPage;
            UpdatePageImage();
            SetProperCallback();
        }
        private static void SetSelectionVar(short index)
        {
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = index;
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
                case Page.Stats: DrawStatsPage(bmpAddress, bmpWidth, bmpHeight); break;
                case Page.Moves: DrawMovesPage(bmpAddress, bmpWidth, bmpHeight); break;
            }
        }
        private unsafe void DrawInfoPage(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            const float winX = 0.03f;
            const float winY = 0.15f;
            const float winW = 0.97f - winX;
            const float winH = 0.85f - winY;
            const float leftColX = winX + 0.02f;
            const float rightColX = winX + 0.52f;
            const float rightColY = winY + 0.03f;
            const float rightColW = 0.95f - rightColX;
            const float rightColH = 0.82f - rightColY;
            const float rightColCenterX = rightColX + (rightColW / 2f);
            const float textStartY = rightColY + 0.02f;
            const float textSpacingY = 0.1f;
            int xpW = (int)(bmpWidth * 0.3f);
            int xpX = RenderUtils.GetCoordinatesForCentering(bmpWidth, xpW, rightColCenterX);
            int xpY = (int)(bmpHeight * (rightColY + 0.61f));
            RenderUtils.FillRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, winX, winY, winX + winW, winY + winH, 15, RenderUtils.Color(128, 215, 135, 255));
            RenderUtils.FillRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, rightColX, rightColY, rightColX + rightColW, rightColY + rightColH, 8, RenderUtils.Color(210, 210, 210, 255));

            Font leftColFont = Font.Default;
            uint[] leftColColors = Font.DefaultWhite_DarkerOutline_I;
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

            PBESpecies species;
            PBEForm form;
            OTInfo ot;
            byte level;
            uint exp;
            if (_pPkmn is not null)
            {
                species = _pPkmn.Species;
                form = _pPkmn.Form;
                ot = _pPkmn.OT;
                level = _pPkmn.Level;
                exp = _pPkmn.EXP;
            }
            else if (_pcPkmn is not null)
            {
                species = _pcPkmn.Species;
                form = _pcPkmn.Form;
                ot = _pcPkmn.OT;
                level = _pcPkmn.Level;
                exp = _pcPkmn.EXP;
            }
            else
            {
                PartyPokemon pPkmn = _bPkmn.PartyPkmn;
                PBEBattlePokemon bPkmn = _bPkmn.Pkmn;
                species = pPkmn.Species;
                form = bPkmn.RevertForm;
                ot = pPkmn.OT;
                level = bPkmn.Level;
                exp = bPkmn.EXP;
            }

            var bs = BaseStats.Get(species, form, true);
            uint toNextLvl;
            if (level >= PkmnConstants.MaxLevel)
            {
                toNextLvl = 0;
                RenderUtils.EXP_SingleLine(bmpAddress, bmpWidth, bmpHeight, xpX, xpY, xpW, 0);
            }
            else
            {
                PBEGrowthRate gr = bs.GrowthRate;
                toNextLvl = PBEEXPTables.GetEXPRequired(gr, (byte)(level + 1)) - exp;
                RenderUtils.EXP_SingleLine(bmpAddress, bmpWidth, bmpHeight, xpX, xpY, xpW, exp, level, gr);
            }

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
            const float winW = 0.75f - winX;
            const float winH = 0.93f - winY;
            const float leftColX = winX + 0.03f;
            const float textStartY = winY + 0.05f;
            const float textSpacingY = 0.1f;
            RenderUtils.FillRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, winX, winY, winX + winW, winY + winH, 15, RenderUtils.Color(145, 225, 225, 255));

            Font leftColFont = Font.Default;
            uint[] leftColColors = Font.DefaultBlack_I;
            uint[] highlightColors = Font.DefaultRed_I;

            void Place(int i, int xOff, string leftColStr, uint[] colors)
            {
                float y = textStartY + (i * textSpacingY);
                leftColFont.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * leftColX) + xOff, (int)(bmpHeight * y), leftColStr, colors);
            }

            PBENature nature;
            DateTime met;
            MapSection loc;
            byte metLvl;
            uint pid;
            IVs ivs;
            if (_pPkmn is not null)
            {
                nature = _pPkmn.Nature;
                met = _pPkmn.MetDate;
                loc = _pPkmn.MetLocation;
                metLvl = _pPkmn.MetLevel;
                pid = _pPkmn.PID;
                ivs = _pPkmn.IndividualValues;
            }
            else if (_pcPkmn is not null)
            {
                nature = _pcPkmn.Nature;
                met = _pcPkmn.MetDate;
                loc = _pcPkmn.MetLocation;
                metLvl = _pcPkmn.MetLevel;
                pid = _pcPkmn.PID;
                ivs = _pcPkmn.IndividualValues;
            }
            else
            {
                PartyPokemon pPkmn = _bPkmn.PartyPkmn;
                nature = pPkmn.Nature;
                met = pPkmn.MetDate;
                loc = pPkmn.MetLocation;
                metLvl = pPkmn.MetLevel;
                pid = pPkmn.PID;
                ivs = pPkmn.IndividualValues;
            }

            string characteristic = Characteristic.GetCharacteristic(pid, ivs) + '.';
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
        private unsafe void DrawStatsPage(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            const float winX = 0.03f;
            const float winY = 0.15f;
            const float winW = 0.97f - winX;
            const float winH = 0.995f - winY;
            const float leftColX = winX + 0.02f;
            const float rightColX = winX + 0.52f;
            const float rightColY = winY + 0.02f;
            const float rightColW = 0.95f - rightColX;
            const float rightColH = 0.535f;
            const float rightColCenterX = rightColX + (rightColW / 2f);
            const float textStartY = rightColY + 0.01f;
            const float textStart2Y = rightColY + 0.13f;
            const float textSpacingY = 0.08f;
            const float abilTextY = textStart2Y + (5.5f * textSpacingY);
            const float abilDescX = leftColX + 0.03f;
            const float abilDescY = textStart2Y + (6.6f * textSpacingY);
            const float abilX = winX + 0.18f;
            const float abilTextX = abilX + 0.03f;
            const float abilY = abilTextY;
            const float abilW = 0.95f - abilX;
            const float abilH = 0.075f;
            int hpW = (int)(bmpWidth * 0.3f);
            int hpX = RenderUtils.GetCoordinatesForCentering(bmpWidth, hpW, rightColCenterX);
            int hpY = (int)(bmpHeight * (rightColY + 0.09f));
            RenderUtils.FillRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, winX, winY, winX + winW, winY + winH, 12, RenderUtils.Color(135, 145, 250, 255));
            // Stats
            RenderUtils.FillRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, rightColX, rightColY, rightColX + rightColW, rightColY + rightColH, 8, RenderUtils.Color(210, 210, 210, 255));
            // Abil
            RenderUtils.FillRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, abilX, abilY, abilX + abilW, abilY + abilH, 5, RenderUtils.Color(210, 210, 210, 255));
            // Abil desc
            RenderUtils.FillRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, leftColX, abilDescY, 0.95f, 0.98f, 5, RenderUtils.Color(210, 210, 210, 255));

            Font leftColFont = Font.Default;
            uint[] leftColColors = Font.DefaultWhite_DarkerOutline_I;
            Font rightColFont = Font.Default;
            uint[] rightColColors = Font.DefaultBlack_I;
            uint[] boostedColors = Font.DefaultRed_Lighter_O;
            uint[] dislikedColors = Font.DefaultCyan_O;

            void PlaceLeftCol(int i, string leftColStr, bool boosted, bool disliked)
            {
                float y;
                if (i == -1)
                {
                    y = abilTextY;
                }
                else if (i == -2)
                {
                    y = textStartY;
                }
                else
                {
                    y = textStart2Y + (i * textSpacingY);
                }
                uint[] colors;
                if (boosted)
                {
                    colors = boostedColors;
                }
                else if (disliked)
                {
                    colors = dislikedColors;
                }
                else
                {
                    colors = leftColColors;
                }
                leftColFont.DrawString(bmpAddress, bmpWidth, bmpHeight, leftColX, y, leftColStr, colors);
            }
            void PlaceRightCol(int i, string rightColStr, uint[] colors)
            {
                float y = i == -2 ? textStartY : textStart2Y + (i * textSpacingY);
                rightColFont.MeasureString(rightColStr, out int strW, out _);
                rightColFont.DrawString(bmpAddress, bmpWidth, bmpHeight,
                    RenderUtils.GetCoordinatesForCentering(bmpWidth, strW, rightColCenterX), (int)(bmpHeight * y), rightColStr, colors);
            }

            BaseStats bs;
            PBEAbility abil;
            PBENature nature;
            IPBEStatCollection evs;
            IVs ivs;
            byte level;
            ushort hp;
            ushort maxHP;
            if (_pPkmn is not null)
            {
                bs = BaseStats.Get(_pPkmn.Species, _pPkmn.Form, true);
                abil = _pPkmn.Ability;
                nature = _pPkmn.Nature;
                evs = _pPkmn.EffortValues;
                ivs = _pPkmn.IndividualValues;
                level = _pPkmn.Level;
                hp = _pPkmn.HP;
                maxHP = _pPkmn.MaxHP;
            }
            else if (_pcPkmn is not null)
            {
                bs = BaseStats.Get(_pcPkmn.Species, _pcPkmn.Form, true);
                abil = _pcPkmn.Ability;
                nature = _pcPkmn.Nature;
                evs = _pcPkmn.EffortValues;
                ivs = _pcPkmn.IndividualValues;
                level = _pcPkmn.Level;
                hp = maxHP = PBEDataUtils.CalculateStat(bs, PBEStat.HP, nature, evs.GetStat(PBEStat.HP), ivs.HP, level, PkmnConstants.PBESettings);
            }
            else
            {
                PartyPokemon pPkmn = _bPkmn.PartyPkmn;
                PBEBattlePokemon bPkmn = _bPkmn.Pkmn;
                bs = BaseStats.Get(pPkmn.Species, bPkmn.RevertForm, true);
                abil = pPkmn.Ability;
                nature = pPkmn.Nature;
                evs = bPkmn.EffortValues;
                ivs = pPkmn.IndividualValues;
                level = bPkmn.Level;
                hp = bPkmn.HP;
                maxHP = bPkmn.MaxHP;
            }
            ushort atk = PBEDataUtils.CalculateStat(bs, PBEStat.Attack, nature, evs.GetStat(PBEStat.Attack), ivs.Attack, level, PkmnConstants.PBESettings);
            ushort def = PBEDataUtils.CalculateStat(bs, PBEStat.Defense, nature, evs.GetStat(PBEStat.Defense), ivs.Defense, level, PkmnConstants.PBESettings);
            ushort spAtk = PBEDataUtils.CalculateStat(bs, PBEStat.SpAttack, nature, evs.GetStat(PBEStat.SpAttack), ivs.SpAttack, level, PkmnConstants.PBESettings);
            ushort spDef = PBEDataUtils.CalculateStat(bs, PBEStat.SpDefense, nature, evs.GetStat(PBEStat.SpDefense), ivs.SpDefense, level, PkmnConstants.PBESettings);
            ushort speed = PBEDataUtils.CalculateStat(bs, PBEStat.Speed, nature, evs.GetStat(PBEStat.Speed), ivs.Speed, level, PkmnConstants.PBESettings);
            PBEStat? favored = nature.GetLikedStat();
            PBEStat? disliked = nature.GetDislikedStat();

            PlaceLeftCol(-2, "HP", false, false);
            PlaceLeftCol(0, "Attack", favored == PBEStat.Attack, disliked == PBEStat.Attack);
            PlaceLeftCol(1, "Defense", favored == PBEStat.Defense, disliked == PBEStat.Defense);
            PlaceLeftCol(2, "Special Attack", favored == PBEStat.SpAttack, disliked == PBEStat.SpAttack);
            PlaceLeftCol(3, "Special Defense", favored == PBEStat.SpDefense, disliked == PBEStat.SpDefense);
            PlaceLeftCol(4, "Speed", favored == PBEStat.Speed, disliked == PBEStat.Speed);
            PlaceLeftCol(-1, "Ability", false, false);

            // HP
            string str = string.Format("{0}/{1}", hp, maxHP);
            PlaceRightCol(-2, str, rightColColors);
            double percent = (double)hp / maxHP;
            RenderUtils.HP_TripleLine(bmpAddress, bmpWidth, bmpHeight, hpX, hpY, hpW, percent);
            // Attack
            str = atk.ToString();
            PlaceRightCol(0, str, rightColColors);
            // Defense
            str = def.ToString();
            PlaceRightCol(1, str, rightColColors);
            // Sp. Attack
            str = spAtk.ToString();
            PlaceRightCol(2, str, rightColColors);
            // Sp. Defense
            str = spDef.ToString();
            PlaceRightCol(3, str, rightColColors);
            // Speed
            str = speed.ToString();
            PlaceRightCol(4, str, rightColColors);
            // Ability
            str = PBELocalizedString.GetAbilityName(abil).English;
            rightColFont.DrawString(bmpAddress, bmpWidth, bmpHeight, abilTextX, abilTextY, str, rightColColors);
            // Ability desc
            str = PBELocalizedString.GetAbilityDescription(abil).English;
            leftColFont.DrawString(bmpAddress, bmpWidth, bmpHeight, abilDescX, abilDescY, str, rightColColors);
        }
        private unsafe void DrawMovesPage(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            const float winX = 0.08f;
            const float winY = 0.15f;
            const float winW = 0.75f - winX;
            const float winH = 0.9f - winY;
            const float moveColX = winX + 0.03f;
            const float moveTextX = moveColX + 0.02f;
            const float moveColW = 0.69f - winX;
            const float itemSpacingY = winH / (PkmnConstants.NumMoves + 0.75f);
            const float moveX = 0.21f;
            const float moveY = 0.03f;
            const float ppX = 0.12f;
            const float ppNumX = 0.35f;
            const float ppY = itemSpacingY / 2;
            const float cancelY = winY + moveY + (PkmnConstants.NumMoves * itemSpacingY);
            RenderUtils.FillRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, winX, winY, winX + winW, winY + winH, 15, RenderUtils.Color(250, 128, 120, 255));

            Font moveFont = Font.Default;
            uint[] moveColors = Font.DefaultWhite_DarkerOutline_I;
            uint[] ppColors = Font.DefaultBlack_I;

            void Place(int i, PBEMove move, int pp, int maxPP)
            {
                PBEMoveData mData = PBEMoveData.Data[move];
                float x = moveTextX;
                float y = winY + moveY + (i * itemSpacingY);
                string str = PBELocalizedString.GetTypeName(mData.Type).English;
                moveFont.DrawString(bmpAddress, bmpWidth, bmpHeight, x, y, str, moveColors);
                x += moveX;
                str = PBELocalizedString.GetMoveName(move).English;
                moveFont.DrawString(bmpAddress, bmpWidth, bmpHeight, x, y, str, moveColors);
                x = moveTextX + ppX;
                y += ppY;
                str = "PP";
                moveFont.DrawString(bmpAddress, bmpWidth, bmpHeight, x, y, str, ppColors);
                x = moveTextX + ppNumX;
                str = string.Format("{0}/{1}", pp, maxPP);
                moveFont.MeasureString(str, out int strW, out _);
                moveFont.DrawString(bmpAddress, bmpWidth, bmpHeight, RenderUtils.GetCoordinatesForCentering(bmpWidth, strW, x), (int)(bmpHeight * y), str, ppColors);

                DrawSelection(i);
            }
            void DrawSelection(int i)
            {
                if (_selectingMove != i)
                {
                    return;
                }
                float x = moveColX;
                float y = winY + moveY + (i * itemSpacingY);
                float w = moveColW;
                float h = i == PkmnConstants.NumMoves ? itemSpacingY / 2 : itemSpacingY;
                RenderUtils.DrawRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, x, y, x + w, y + h, 5, RenderUtils.Color(48, 180, 255, 200));
            }

            // Moves
            if (_pPkmn is not null)
            {
                Moveset moves = _pPkmn.Moveset;
                for (int m = 0; m < PkmnConstants.NumMoves; m++)
                {
                    Moveset.MovesetSlot slot = moves[m];
                    PBEMove move = slot.Move;
                    if (move == PBEMove.None)
                    {
                        continue;
                    }
                    int pp = slot.PP;
                    int maxPP = PBEDataUtils.CalcMaxPP(move, slot.PPUps, PkmnConstants.PBESettings);
                    Place(m, move, pp, maxPP);
                }
            }
            else if (_pcPkmn is not null)
            {
                BoxMoveset moves = _pcPkmn.Moveset;
                for (int m = 0; m < PkmnConstants.NumMoves; m++)
                {
                    BoxMoveset.BoxMovesetSlot slot = moves[m];
                    PBEMove move = slot.Move;
                    if (move == PBEMove.None)
                    {
                        continue;
                    }
                    int maxPP = PBEDataUtils.CalcMaxPP(move, slot.PPUps, PkmnConstants.PBESettings);
                    Place(m, move, maxPP, maxPP);
                }
            }
            else
            {
                PBEBattlePokemon bPkmn = _bPkmn.Pkmn;
                PBEBattleMoveset moves = bPkmn.Status2.HasFlag(PBEStatus2.Transformed) ? bPkmn.TransformBackupMoves : bPkmn.Moves;
                for (int m = 0; m < PkmnConstants.NumMoves; m++)
                {
                    PBEBattleMoveset.PBEBattleMovesetSlot slot = moves[m];
                    PBEMove move = slot.Move;
                    if (move == PBEMove.None)
                    {
                        continue;
                    }
                    int pp = slot.PP;
                    int maxPP = slot.MaxPP;
                    Place(m, move, pp, maxPP);
                }
            }

            // Cancel or new move
            if (_learningMove != PBEMove.None)
            {
                uint[] learnColors = Font.DefaultBlue_I;
                PBEMoveData mData = PBEMoveData.Data[_learningMove];
                float x = moveTextX;
                string str = PBELocalizedString.GetTypeName(mData.Type).English;
                moveFont.DrawString(bmpAddress, bmpWidth, bmpHeight, x, cancelY, str, learnColors);
                x += moveX;
                str = PBELocalizedString.GetMoveName(_learningMove).English;
                moveFont.DrawString(bmpAddress, bmpWidth, bmpHeight, x, cancelY, str, learnColors);
                DrawSelection(PkmnConstants.NumMoves);
            }
            else
            {
                if (_selectingMove != -1)
                {
                    string str = "Cancel";
                    moveFont.DrawString(bmpAddress, bmpWidth, bmpHeight, moveTextX, cancelY, str, moveColors);
                    DrawSelection(PkmnConstants.NumMoves);
                }
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
            if (InputManager.IsPressed(Key.Right))
            {
                SwapPage(Page.Stats);
                return;
            }
        }
        private void CB_StatsPage()
        {
            if (InputManager.IsPressed(Key.B))
            {
                CloseSummaryMenu();
                return;
            }
            if (InputManager.IsPressed(Key.Left))
            {
                SwapPage(Page.Personal);
                return;
            }
            if (InputManager.IsPressed(Key.Right))
            {
                SwapPage(Page.Moves);
                return;
            }
        }
        private void CB_MovesPage()
        {
            if (_selectingMove != -1)
            {
                if (InputManager.IsPressed(Key.A))
                {
                    if (_mode == Mode.LearnMove)
                    {
                        SetSelectionVar((short)_selectingMove);
                        CloseSummaryMenu();
                    }
                    else
                    {
                        if (_selectingMove == PkmnConstants.NumMoves)
                        {
                            _selectingMove = -1;
                            UpdatePageImage();
                        }
                    }
                    return;
                }
                if (InputManager.IsPressed(Key.B))
                {
                    _selectingMove = -1;
                    UpdatePageImage();
                    return;
                }
                if (InputManager.IsPressed(Key.Up))
                {
                    if (_selectingMove != 0)
                    {
                        _selectingMove--;
                        UpdatePageImage();
                    }
                    return;
                }
                if (InputManager.IsPressed(Key.Down))
                {
                    if (_selectingMove != PkmnConstants.NumMoves)
                    {
                        _selectingMove++;
                        UpdatePageImage();
                    }
                    return;
                }
            }
            else
            {
                if (InputManager.IsPressed(Key.A))
                {
                    _selectingMove = 0;
                    UpdatePageImage();
                    return;
                }
                if (InputManager.IsPressed(Key.B))
                {
                    CloseSummaryMenu();
                    return;
                }
                if (InputManager.IsPressed(Key.Left))
                {
                    SwapPage(Page.Stats);
                    return;
                }
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
