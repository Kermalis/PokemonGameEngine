using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonBattleEngine.DefaultData;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.World;
using Silk.NET.OpenGL;
using System;
using System.Numerics;

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

        private const short NOT_SELECTING_MOVES = -1;
        public const short NO_MOVE_CHOSEN = -1;
        /// <summary>Index of the cancel button, or the index of the move to learn</summary>
        private const short CANCEL_BUTTON_INDEX = PkmnConstants.NumMoves;

        private readonly Mode _mode;
        private Page _page;
        private readonly BoxPokemon _pcPkmn;
        private readonly PartyPokemon _pPkmn;
        private readonly SpritedBattlePokemon _bPkmn;
        private readonly PBEMove _learningMove;
        private short _moveSelection;

        private FadeColorTransition _fadeTransition;
        private Action _onClosed;

        private AnimatedImage _pkmnImage;
        private readonly WriteableImage _pageImage;
        private const float PageImageWidth = 0.55f;
        private const float PageImageHeight = 0.95f;

        #region Open & Close GUI

        public SummaryGUI(object pkmn, Mode mode, Action onClosed, PBEMove learningMove = PBEMove.None)
        {
            _mode = mode;
            if (mode == Mode.LearnMove)
            {
                SetSelectionVar(NO_MOVE_CHOSEN);
                _page = Page.Moves;
                _moveSelection = 0;
                _learningMove = learningMove;
            }
            else
            {
                _page = Page.Info;
                _moveSelection = NOT_SELECTING_MOVES;
            }

            _pageImage = new WriteableImage(Size2D.FromRelative(PageImageWidth, PageImageHeight, Display.RenderWidth, Display.RenderHeight));

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

            _fadeTransition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInSummary);
        }

        private void SetExitFadeOutCallback()
        {
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutSummary);
        }

        private void CB_FadeInSummary()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            PlayPkmnCry();
            SetProperCallback();
        }
        private void CB_FadeOutSummary()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _pageImage.DeductReference();
            _pkmnImage.DeductReference();
            _onClosed();
            _onClosed = null;
        }

        private void RenderFading()
        {
            Render();
            _fadeTransition.Render();
        }

        #endregion

        private int GetNumMovesPkmnHas()
        {
            if (_pPkmn is not null)
            {
                return _pPkmn.Moveset.CountMoves();
            }
            else if (_pcPkmn is not null)
            {
                return _pcPkmn.Moveset.CountMoves();
            }
            return _bPkmn.Pkmn.Moves.CountMoves();
        }

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
            _pkmnImage?.DeductReference();
            _pkmnImage = PokemonImageLoader.GetPokemonImage(species, form, gender, shiny, false, false, pid, egg);
        }
        private void PlayPkmnCry()
        {
            PBESpecies species;
            PBEForm form;
            float hpPercentage;
            if (_pPkmn is not null)
            {
                if (_pPkmn.IsEgg)
                {
                    return;
                }
                species = _pPkmn.Species;
                form = _pPkmn.Form;
                hpPercentage = (float)_pPkmn.HP / _pPkmn.MaxHP;
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
            SoundControl.PlayCryFromHP(species, form, hpPercentage);
        }
        private void SetProperCallback()
        {
            MainCallbackDelegate cb;
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

        private void UpdatePageImage()
        {
            GL gl = Display.OpenGL;
            _pageImage.PushFrameBuffer(gl);
            GLHelper.ClearColor(gl, Colors.Transparent);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            GUIString.CreateAndRenderOneTimeString(_page.ToString(), Font.Default, FontColors.DefaultBlack_I, new Pos2D(0, 0), scale: 2);

            switch (_page)
            {
                case Page.Info: DrawInfoPage(); break;
                case Page.Personal: DrawPersonalPage(); break;
                case Page.Stats: DrawStatsPage(); break;
                case Page.Moves: DrawMovesPage(); break;
            }
            GLHelper.PopFrameBuffer(gl);
        }
        private void DrawInfoPage()
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
            uint xpW = (uint)Renderer.RelXToAbsX(0.3f);
            int xpX = Renderer.GetCoordinatesForCentering(GLHelper.CurrentWidth, xpW, rightColCenterX);
            int xpY = Renderer.RelYToAbsY(rightColY + 0.61f);
            GUIRenderer.Instance.FillRectangle(Colors.V4FromRGB(128, 215, 135), new Rect2D(Pos2D.FromRelative(winX, winY), Size2D.FromRelative(winW, winH))); // TODO: ROUNDED 15
            GUIRenderer.Instance.FillRectangle(Colors.V4FromRGB(210, 210, 210), new Rect2D(Pos2D.FromRelative(rightColX, rightColY), Size2D.FromRelative(rightColW, rightColH))); // TODO: ROUNDED 8

            Font leftColFont = Font.Default;
            Vector4[] leftColColors = FontColors.DefaultWhite_DarkerOutline_I;
            Font rightColFont = Font.Default;
            Vector4[] rightColColors = FontColors.DefaultBlack_I;

            void PlaceLeftCol(int i, string leftColStr)
            {
                float y = textStartY + (i * textSpacingY);
                GUIString.CreateAndRenderOneTimeString(leftColStr, leftColFont, leftColColors, Pos2D.FromRelative(leftColX, y));
            }
            void PlaceRightCol(int i, string rightColStr, Vector4[] colors)
            {
                float y = textStartY + (i * textSpacingY);
                Size2D size = rightColFont.MeasureString(rightColStr);
                GUIString.CreateAndRenderOneTimeString(rightColStr, rightColFont, colors,
                    new Pos2D(Renderer.GetCoordinatesForCentering(GLHelper.CurrentWidth, size.Width, rightColCenterX), Renderer.RelYToAbsY(y)));
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
                Renderer.EXP_SingleLine(xpX, xpY, xpW, 0);
            }
            else
            {
                PBEGrowthRate gr = bs.GrowthRate;
                toNextLvl = PBEDataProvider.Instance.GetEXPRequired(gr, (byte)(level + 1)) - exp;
                Renderer.EXP_SingleLine(xpX, xpY, xpW, exp, level, gr);
            }

            // Species
            string str = PBEDataProvider.Instance.GetSpeciesName(species).English;
            PlaceRightCol(0, str, rightColColors);
            // Types
            str = PBEDataProvider.Instance.GetTypeName(bs.Type1).English;
            if (bs.Type2 != PBEType.None)
            {
                str += ' ' + PBEDataProvider.Instance.GetTypeName(bs.Type2).English;
            }
            PlaceRightCol(1, str, rightColColors);
            // OT
            str = ot.TrainerName;
            PlaceRightCol(2, str, ot.TrainerIsFemale ? FontColors.DefaultRed_I : FontColors.DefaultBlue_I);
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
        private void DrawPersonalPage()
        {
            const float winX = 0.08f;
            const float winY = 0.15f;
            const float winW = 0.75f - winX;
            const float winH = 0.93f - winY;
            const float leftColX = winX + 0.03f;
            const float textStartY = winY + 0.05f;
            const float textSpacingY = 0.1f;
            GUIRenderer.Instance.FillRectangle(Colors.V4FromRGB(145, 225, 225), new Rect2D(Pos2D.FromRelative(winX, winY), Size2D.FromRelative(winW, winH))); // TODO: ROUNDED 15

            Font leftColFont = Font.Default;
            Vector4[] leftColColors = FontColors.DefaultBlack_I;
            Vector4[] highlightColors = FontColors.DefaultRed_I;

            void Place(int i, int xOff, string leftColStr, Vector4[] colors)
            {
                float y = textStartY + (i * textSpacingY);
                var pos = Pos2D.FromRelative(leftColX, y);
                pos.X += xOff;
                GUIString.CreateAndRenderOneTimeString(leftColStr, leftColFont, colors, pos);
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
            string str = PBEDataProvider.Instance.GetNatureName(nature).English + ' ';
            Place(0, 0, str, highlightColors);
            Size2D strS = leftColFont.MeasureString(str);
            str = "nature.";
            Place(0, (int)strS.Width, str, leftColColors);
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
                strS = leftColFont.MeasureString(str);
                str = flavor.Value.ToString() + ' ';
                Place(6, (int)strS.Width, str, highlightColors);
                Size2D strS2 = leftColFont.MeasureString(str);
                str = "food.";
                Place(6, (int)(strS.Width + strS2.Width), str, leftColColors);
            }
            else
            {
                str = "Likes all food.";
                Place(6, 0, str, leftColColors);
            }
        }
        private void DrawStatsPage()
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
            uint hpW = (uint)Renderer.RelXToAbsX(0.3f);
            int hpX = Renderer.GetCoordinatesForCentering(GLHelper.CurrentWidth, hpW, rightColCenterX);
            int hpY = Renderer.RelYToAbsY(rightColY + 0.09f);
            GUIRenderer.Instance.FillRectangle(Colors.V4FromRGB(135, 145, 250), new Rect2D(Pos2D.FromRelative(winX, winY), Size2D.FromRelative(winW, winH))); // TODO: ROUNDED 12
            // Stats
            GUIRenderer.Instance.FillRectangle(Colors.V4FromRGB(210, 210, 210), new Rect2D(Pos2D.FromRelative(rightColX, rightColY), Size2D.FromRelative(rightColW, rightColH))); // TODO: ROUNDED 8
            // Abil
            GUIRenderer.Instance.FillRectangle(Colors.V4FromRGB(210, 210, 210), new Rect2D(Pos2D.FromRelative(abilX, abilY), Size2D.FromRelative(abilW, abilH))); // TODO: ROUNDED 5
            // Abil desc
            GUIRenderer.Instance.FillRectangle(Colors.V4FromRGB(210, 210, 210), new Rect2D(Pos2D.FromRelative(leftColX, abilDescY), Pos2D.FromRelative(0.945f, 0.97f))); // TODO: ROUNDED 5

            Font leftColFont = Font.Default;
            Vector4[] leftColColors = FontColors.DefaultWhite_DarkerOutline_I;
            Font rightColFont = Font.Default;
            Vector4[] rightColColors = FontColors.DefaultBlack_I;
            Vector4[] boostedColors = FontColors.DefaultRed_Lighter_O;
            Vector4[] dislikedColors = FontColors.DefaultCyan_O;

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
                Vector4[] colors;
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
                GUIString.CreateAndRenderOneTimeString(leftColStr, leftColFont, colors, Pos2D.FromRelative(leftColX, y));
            }
            void PlaceRightCol(int i, string rightColStr, Vector4[] colors)
            {
                float y = i == -2 ? textStartY : textStart2Y + (i * textSpacingY);
                Size2D s = rightColFont.MeasureString(rightColStr);
                GUIString.CreateAndRenderOneTimeString(rightColStr, rightColFont, colors,
                    new Pos2D(Renderer.GetCoordinatesForCentering(GLHelper.CurrentWidth, s.Width, rightColCenterX), Renderer.RelYToAbsY(y)));
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
            float percent = (float)hp / maxHP;
            Renderer.HP_TripleLine(hpX, hpY, hpW, percent);
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
            str = PBEDataProvider.Instance.GetAbilityName(abil).English;
            GUIString.CreateAndRenderOneTimeString(str, rightColFont, rightColColors, Pos2D.FromRelative(abilTextX, abilTextY));
            // Ability desc
            str = PBEDefaultDataProvider.Instance.GetAbilityDescription(abil).English;
            GUIString.CreateAndRenderOneTimeString(str, leftColFont, rightColColors, Pos2D.FromRelative(abilDescX, abilDescY));
        }
        private void DrawMovesPage()
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
            GUIRenderer.Instance.FillRectangle(Colors.V4FromRGB(250, 128, 120), new Rect2D(Pos2D.FromRelative(winX, winY), Size2D.FromRelative(winW, winH))); // TODO: ROUNDED 15

            Font moveFont = Font.Default;
            Vector4[] moveColors = FontColors.DefaultWhite_DarkerOutline_I;
            Vector4[] ppColors = FontColors.DefaultBlack_I;

            void Place(int i, PBEMove move, int pp, int maxPP)
            {
                IPBEMoveData mData = PBEDataProvider.Instance.GetMoveData(move);
                float x = moveTextX;
                float y = winY + moveY + (i * itemSpacingY);
                string str = PBEDataProvider.Instance.GetTypeName(mData.Type).English;
                GUIString.CreateAndRenderOneTimeString(str, moveFont, moveColors, Pos2D.FromRelative(x, y));
                x += moveX;
                str = PBEDataProvider.Instance.GetMoveName(move).English;
                GUIString.CreateAndRenderOneTimeString(str, moveFont, moveColors, Pos2D.FromRelative(x, y));
                x = moveTextX + ppX;
                y += ppY;
                str = "PP";
                GUIString.CreateAndRenderOneTimeString(str, moveFont, ppColors, Pos2D.FromRelative(x, y));
                x = moveTextX + ppNumX;
                str = string.Format("{0}/{1}", pp, maxPP);
                Size2D s = moveFont.MeasureString(str);
                GUIString.CreateAndRenderOneTimeString(str, moveFont, ppColors,
                    new Pos2D(Renderer.GetCoordinatesForCentering(GLHelper.CurrentWidth, s.Width, x), Renderer.RelYToAbsY(y)));

                DrawSelection(i);
            }
            // Draws the selection border only if this is the selected move
            void DrawSelection(int i)
            {
                if (_moveSelection != i)
                {
                    return;
                }
                float x = moveColX;
                float y = winY + moveY + (i * itemSpacingY);
                float w = moveColW;
                float h = i == CANCEL_BUTTON_INDEX ? itemSpacingY / 2 : itemSpacingY;
                GUIRenderer.Instance.DrawRectangle(Colors.FromRGBA(48, 180, 255, 200), new Rect2D(Pos2D.FromRelative(x, y), Size2D.FromRelative(w, h))); // TODO: ROUNDED 5
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
                Vector4[] learnColors = FontColors.DefaultBlue_I;
                IPBEMoveData mData = PBEDataProvider.Instance.GetMoveData(_learningMove);
                float x = moveTextX;
                string str = PBEDataProvider.Instance.GetTypeName(mData.Type).English;
                GUIString.CreateAndRenderOneTimeString(str, moveFont, learnColors, Pos2D.FromRelative(x, cancelY));
                x += moveX;
                str = PBEDataProvider.Instance.GetMoveName(_learningMove).English;
                GUIString.CreateAndRenderOneTimeString(str, moveFont, learnColors, Pos2D.FromRelative(x, cancelY));
                DrawSelection(CANCEL_BUTTON_INDEX);
            }
            else
            {
                // Place cancel button if we're selecting a move
                if (_moveSelection != NOT_SELECTING_MOVES)
                {
                    string str = "Cancel";
                    GUIString.CreateAndRenderOneTimeString(str, moveFont, moveColors, Pos2D.FromRelative(moveTextX, cancelY));
                    DrawSelection(CANCEL_BUTTON_INDEX);
                }
            }
        }

        private void CB_InfoPage()
        {
            HandleInputs_InfoPage();
            Render();
        }
        private void CB_PersonalPage()
        {
            HandleInputs_PersonalPage();
            Render();
        }
        private void CB_StatsPage()
        {
            HandleInputs_StatsPage();
            Render();
        }
        private void CB_MovesPage()
        {
            if (_moveSelection == NOT_SELECTING_MOVES)
            {
                HandleInputs_MovesPage();
            }
            else
            {
                HandleInputs_MovesPage_SelectingMove();
            }
            Render();
        }

        private void HandleInputs_InfoPage()
        {
            if (InputManager.IsPressed(Key.B))
            {
                SetExitFadeOutCallback();
                return;
            }
            if (InputManager.IsPressed(Key.Right))
            {
                SwapPage(Page.Personal);
                return;
            }
        }
        private void HandleInputs_PersonalPage()
        {
            if (InputManager.IsPressed(Key.B))
            {
                SetExitFadeOutCallback();
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
        private void HandleInputs_StatsPage()
        {
            if (InputManager.IsPressed(Key.B))
            {
                SetExitFadeOutCallback();
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
        private void HandleInputs_MovesPage()
        {
            // Start selecting a move
            if (InputManager.IsPressed(Key.A))
            {
                _moveSelection = 0;
                UpdatePageImage();
                return;
            }
            // Exit summary
            if (InputManager.IsPressed(Key.B))
            {
                SetExitFadeOutCallback();
                return;
            }
            // Go to stats page
            if (InputManager.IsPressed(Key.Left))
            {
                SwapPage(Page.Stats);
                return;
            }
        }
        private void HandleInputs_MovesPage_SelectingMove()
        {
            // Choose selected move
            if (InputManager.IsPressed(Key.A))
            {
                if (_mode == Mode.LearnMove)
                {
                    if (_moveSelection == CANCEL_BUTTON_INDEX)
                    {
                        _moveSelection = NO_MOVE_CHOSEN;
                    }
                    SetSelectionVar(_moveSelection);
                    SetExitFadeOutCallback();
                }
                else // Regular modes, only cancel does anything
                {
                    if (_moveSelection == CANCEL_BUTTON_INDEX)
                    {
                        _moveSelection = NOT_SELECTING_MOVES;
                        UpdatePageImage();
                    }
                }
                return;
            }
            // Stop selecting a move
            if (InputManager.IsPressed(Key.B))
            {
                if (_mode == Mode.LearnMove)
                {
                    SetSelectionVar(NO_MOVE_CHOSEN);
                    SetExitFadeOutCallback();
                }
                else // Regular modes
                {
                    _moveSelection = NOT_SELECTING_MOVES;
                    UpdatePageImage();
                }
                return;
            }
            // Go up a move
            if (InputManager.IsPressed(Key.Up))
            {
                if (_moveSelection == CANCEL_BUTTON_INDEX)
                {
                    _moveSelection = (short)(GetNumMovesPkmnHas() - 1);
                    UpdatePageImage();
                }
                else if (_moveSelection > 0)
                {
                    _moveSelection--;
                    UpdatePageImage();
                }
                return;
            }
            // Go down a move
            if (InputManager.IsPressed(Key.Down))
            {
                if (_moveSelection != CANCEL_BUTTON_INDEX)
                {
                    _moveSelection++;
                    if (_moveSelection >= GetNumMovesPkmnHas())
                    {
                        _moveSelection = CANCEL_BUTTON_INDEX;
                    }
                    UpdatePageImage();
                }
                return;
            }
        }

        private void Render()
        {
            GL gl = Display.OpenGL;
            //Renderer.ThreeColorBackground(dst, dstW, dstH, Renderer.Color(215, 231, 230, 255), Renderer.Color(231, 163, 0, 255), Renderer.Color(242, 182, 32, 255));
            GLHelper.ClearColor(gl, Colors.FromRGB(31, 31, 31));
            gl.Clear(ClearBufferMask.ColorBufferBit);

            _pkmnImage.Update();
            _pkmnImage.Render(Pos2D.CenterXBottomY(0.2f, 0.6f, _pkmnImage.Size));

            _pageImage.Render(Pos2D.FromRelative(1f - PageImageWidth, 1f - PageImageHeight));
        }
    }
}
