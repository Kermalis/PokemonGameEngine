using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed class BattlePokemon
    {
        public PartyPokemon PartyPkmn { get; }
        public PBEBattlePokemon PBEPkmn { get; }
        public uint DisguisedPID { get; set; }
        public Image Mini { get; private set; }
        public WriteableImage InfoBarImg { get; }
        private readonly bool _useBackImage;
        private readonly bool _useKnownInfo;

        private BattlePokemon(PBEBattlePokemon pbePkmn, PartyPokemon pPkmn, bool backImage, bool useKnownInfo)
        {
            PartyPkmn = pPkmn;
            PBEPkmn = pbePkmn;
            DisguisedPID = pPkmn.PID; // By default, use our own PID (for example, wild disguised pkmn)
            _useBackImage = backImage;
            _useKnownInfo = useKnownInfo;
            InfoBarImg = new WriteableImage(new Size2D(100, useKnownInfo ? 30u : 42));
            UpdateInfoBar();
            UpdateMini();
        }

        public static BattlePokemon CreateForTrainerMon(PBEBattlePokemon pbePkmn, PartyPokemon pPkmn, bool backImage, bool useKnownInfo)
        {
            return new BattlePokemon(pbePkmn, pPkmn, backImage, useKnownInfo);
        }
        public static BattlePokemon CreateForWildMon(PBEBattlePokemon pbePkmn, PartyPokemon pPkmn, bool backImage, bool useKnownInfo, PkmnPosition wildPos)
        {
            var bPkmn = new BattlePokemon(pbePkmn, pPkmn, backImage, useKnownInfo);
            bPkmn.UpdateVisuals(wildPos.Sprite, true, true, false, true, true); // wildPos has the pkmn already set up so we need to set the img (mini was updated in constructor)
            wildPos.BattlePkmn = bPkmn;
            bPkmn.UpdateAnimationSpeed(wildPos.Sprite.AnimImage); // Ensure the proper speed is set upon entering battle (roamers can be low hp or have status, for example)
            return bPkmn;
        }

        public void UpdateDisguisedPID(BattlePokemonParty bParty)
        {
            if (PBEPkmn.Status2.HasFlag(PBEStatus2.Disguised))
            {
                PBEBattlePokemon p = PBEPkmn.GetPkmnWouldDisguiseAs();
                DisguisedPID = p is null ? PartyPkmn.PID : bParty[p].PartyPkmn.PID;
            }
            else
            {
                DisguisedPID = PartyPkmn.PID; // Set back to normal
            }
        }
        public void UpdateAnimationSpeed(AnimatedImage animImg)
        {
            PBEBattlePokemon pkmn = PBEPkmn;
            PBEStatus1 s = pkmn.Status1;
            if (s == PBEStatus1.Frozen)
            {
                animImg.IsPaused = true;
            }
            else
            {
                bool shouldBeSlowed = s == PBEStatus1.Paralyzed || s == PBEStatus1.Asleep || pkmn.HPPercentage <= 0.25f;
                animImg.SpeedModifier = shouldBeSlowed ? 0.5f : 1f;
                animImg.IsPaused = false;
            }
        }

        private void UpdateMini()
        {
            Mini?.DeductReference(); // Will be null on creation
            Mini = PokemonImageLoader.GetMini(PBEPkmn.KnownSpecies, PBEPkmn.KnownForm, PBEPkmn.KnownGender, PBEPkmn.KnownShiny, PartyPkmn.IsEgg);
        }

        public void UpdateVisuals(BattleSprite sprite, bool spriteImg, bool spriteImgIfSubstituted, bool mini, bool visibility, bool color)
        {
            if (mini)
            {
                UpdateMini();
            }

            PBEStatus2 status2 = _useKnownInfo ? PBEPkmn.KnownStatus2 : PBEPkmn.Status2;
            bool substitute = status2.HasFlag(PBEStatus2.Substitute);
            if (spriteImg)
            {
                UpdateImageIfRequired(sprite, substitute, status2, spriteImgIfSubstituted);
            }

            if (visibility)
            {
                sprite.IsVisible = ShouldBeVisible(substitute, status2);
            }

            if (color)
            {
                UpdateMaskColor(sprite, substitute, PBEPkmn.Status1);
            }
        }
        private void UpdateImageIfRequired(BattleSprite sprite, bool substitute, PBEStatus2 status2, bool spriteImgIfSubstituted)
        {
            AnimatedImage newImg;
            if (substitute)
            {
                if (!spriteImgIfSubstituted)
                {
                    return; // If behind a substitute, only update when requested (like when the substitute breaks, otherwise we don't care)
                }
                newImg = PokemonImageLoader.GetSubstituteImage(_useBackImage);
            }
            else
            {
                newImg = PokemonImageLoader.GetPokemonImage(PBEPkmn.KnownSpecies, PBEPkmn.KnownForm, PBEPkmn.KnownGender, PBEPkmn.KnownShiny,
                    status2.HasFlag(PBEStatus2.Disguised) ? DisguisedPID : PartyPkmn.PID, _useBackImage);
            }
            // Will deduct reference on the old image
            sprite.UpdateImage(newImg);
        }
        private static bool ShouldBeVisible(bool substitute, PBEStatus2 status2)
        {
            if (substitute)
            {
                return true; // Substitute is always visible even if you're away
            }
            if (status2.HasFlag(PBEStatus2.Airborne)
                || status2.HasFlag(PBEStatus2.ShadowForce)
                || status2.HasFlag(PBEStatus2.Underground)
                || status2.HasFlag(PBEStatus2.Underwater))
            {
                return false; // Invisible if you're not substituted and you're away
            }
            return true; // Visible
        }
        private static void UpdateMaskColor(BattleSprite sprite, bool substitute, PBEStatus1 status1)
        {
            Vector3? color = null;
            bool animateIt = false;
            if (!substitute)
            {
                switch (status1)
                {
                    case PBEStatus1.BadlyPoisoned: color = Colors.FromRGB(100, 20, 200); animateIt = true; break;
                    case PBEStatus1.Burned: color = Colors.FromRGB(180, 0, 0); animateIt = true; break;
                    case PBEStatus1.Frozen: color = Colors.FromRGB(0, 120, 180); break;
                    case PBEStatus1.Paralyzed: color = Colors.FromRGB(240, 170, 0); animateIt = true; break;
                    case PBEStatus1.Poisoned: color = Colors.FromRGB(190, 50, 220); animateIt = true; break;
                }
            }

            sprite.MaskColor = color;
            sprite.AnimateMaskColor = animateIt;
            sprite.MaskColorAmt = !animateIt && color is not null ? BattleSprite.MASK_COLOR_AMPLITUDE : 0f;
        }

        public void UpdateInfoBar()
        {
            GL gl = Display.OpenGL;
            FrameBuffer oldFBO = FrameBuffer.Current;
            InfoBarImg.FrameBuffer.Use();
            gl.ClearColor(Colors.FromRGBA(48, 48, 48, 128));
            gl.Clear(ClearBufferMask.ColorBufferBit);

            // Nickname
            GUIString.CreateAndRenderOneTimeString(PBEPkmn.KnownNickname, Font.DefaultSmall, FontColors.DefaultWhite_I, new Pos2D(2, 3));
            // Gender
            PBEGender gender = _useKnownInfo && !PBEPkmn.KnownStatus2.HasFlag(PBEStatus2.Transformed) ? PBEPkmn.KnownGender : PBEPkmn.Gender;
            GUIString.CreateAndRenderOneTimeGenderString(gender, Font.Default, new Pos2D(51, -2));
            // Level
            const int lvX = 62;
            GUIString.CreateAndRenderOneTimeString("[LV]", Font.PartyNumbers, FontColors.DefaultWhite_I, new Pos2D(lvX, 3));
            GUIString.CreateAndRenderOneTimeString(PBEPkmn.Level.ToString(), Font.PartyNumbers, FontColors.DefaultWhite_I, new Pos2D(lvX + 12, 3));
            // Caught
            if (_useKnownInfo && PBEPkmn.IsWild && Game.Instance.Save.Pokedex.IsCaught(PBEPkmn.KnownSpecies))
            {
                GUIString.CreateAndRenderOneTimeString("*", Font.Default, FontColors.DefaultRed_O, new Pos2D(2, 12));
            }
            // Status
            PBEStatus1 status = PBEPkmn.Status1;
            if (status != PBEStatus1.None)
            {
                GUIString.CreateAndRenderOneTimeString(status.ToString(), Font.DefaultSmall, FontColors.DefaultWhite_I, new Pos2D(30, 13));
            }
            // HP
            if (!_useKnownInfo)
            {
                string str = PBEPkmn.HP.ToString();
                Size2D strS = Font.PartyNumbers.MeasureString(str);
                GUIString.CreateAndRenderOneTimeString(str, Font.PartyNumbers, FontColors.DefaultWhite_I, new Pos2D(45 - (int)strS.Width, 28));
                GUIString.CreateAndRenderOneTimeString("/" + PBEPkmn.MaxHP, Font.PartyNumbers, FontColors.DefaultWhite_I, new Pos2D(46, 28));
            }

            const int lineStartX = 9;
            const int lineW = 82;
            Renderer.HP_TripleLine(lineStartX, 23, lineW, PBEPkmn.HPPercentage);

            // EXP
            if (!_useKnownInfo)
            {
                Renderer.EXP_SingleLine(lineStartX, 37, lineW, PBEPkmn.EXP, PBEPkmn.Level, PBEPkmn.Species, PBEPkmn.RevertForm);
            }

            oldFBO.Use();
        }

        public void Delete()
        {
            InfoBarImg.DeductReference();
            Mini.DeductReference();
        }
    }
}
