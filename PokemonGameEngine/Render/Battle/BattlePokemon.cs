using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Battle
{
    internal sealed class BattlePokemon
    {
        public PartyPokemon PartyPkmn { get; }
        public PBEBattlePokemon PBEPkmn { get; }
        public BattlePokemonParty BParty { get; }
        private readonly bool _useBackImage;
        private readonly bool _useKnownInfo;

        public uint DisguisedPID { get; private set; }
        public Image Mini { get; private set; }
        public FrameBuffer InfoBar { get; }
        public PkmnPosition Pos { get; private set; }

        private BattlePokemon(PBEBattlePokemon pbePkmn, PartyPokemon pPkmn, BattlePokemonParty bParty, bool backImage, bool useKnownInfo)
        {
            PartyPkmn = pPkmn;
            PBEPkmn = pbePkmn;
            BParty = bParty;
            _useBackImage = backImage;
            _useKnownInfo = useKnownInfo;

            DisguisedPID = pPkmn.PID; // By default, use our own PID (for example, wild disguised pkmn)
            UpdateMini();
            InfoBar = new FrameBuffer().AddColorTexture(new Vec2I(100, useKnownInfo ? 30 : 42));
            UpdateInfoBar();
        }

        public static BattlePokemon CreateForTrainerMon(PBEBattlePokemon pbePkmn, PartyPokemon pPkmn, BattlePokemonParty bParty, bool backImage, bool useKnownInfo)
        {
            return new BattlePokemon(pbePkmn, pPkmn, bParty, backImage, useKnownInfo);
        }
        public static BattlePokemon CreateForWildMon(PBEBattlePokemon pbePkmn, PartyPokemon pPkmn, BattlePokemonParty bParty, bool backImage, bool useKnownInfo, PkmnPosition wildPos)
        {
            // wildPos has the pkmn already set up so we need to set the img (mini was updated in constructor)
            // The proper animation speed is set by UpdateSprite() upon entering battle (roamers can be low hp or have status, for example)
            var bPkmn = new BattlePokemon(pbePkmn, pPkmn, bParty, backImage, useKnownInfo);
            bPkmn.AttachPos(wildPos);
            bPkmn.UpdateSprite(img: true, imgIfSubstituted: true, visibility: true, color: true);
            return bPkmn;
        }

        public void AttachPos(PkmnPosition pos)
        {
            Pos = pos;
            pos.BattlePkmn = this;
        }
        public PkmnPosition DetachPos()
        {
            PkmnPosition oldPos = Pos;
            Pos.BattlePkmn = null;
            Pos = null;
            return oldPos;
        }

        public void UpdateDisguisedPID()
        {
            if (PBEPkmn.Status2.HasFlag(PBEStatus2.Disguised))
            {
                DisguisedPID = BParty[PBEPkmn.GetPkmnWouldDisguiseAs()].PartyPkmn.PID;
            }
            else
            {
                DisguisedPID = PartyPkmn.PID; // Set back to normal
            }
        }
        public void UpdateAnimationSpeed()
        {
            AnimatedImage img = Pos.Sprite.Image;
            PBEBattlePokemon pkmn = PBEPkmn;
            PBEStatus1 s = pkmn.Status1;
            if (s == PBEStatus1.Frozen)
            {
                img.IsPaused = true;
            }
            else
            {
                bool shouldBeSlowed = s == PBEStatus1.Paralyzed || s == PBEStatus1.Asleep || pkmn.HPPercentage <= 0.25f;
                img.SpeedModifier = shouldBeSlowed ? 0.5f : 1f;
                img.IsPaused = false;
            }
        }
        public void UpdateMini()
        {
            Mini?.DeductReference(); // Will be null on creation
            if (PartyPkmn.IsEgg)
            {
                Mini = PokemonImageLoader.GetEggMini();
            }
            else
            {
                Mini = PokemonImageLoader.GetMini(PBEPkmn.KnownSpecies, PBEPkmn.KnownForm, PBEPkmn.KnownGender, PBEPkmn.KnownShiny);
            }
        }

        public void UpdateSprite(bool img = false, bool imgIfSubstituted = false, bool visibility = false, bool color = false)
        {
            PBEStatus2 status2 = _useKnownInfo ? PBEPkmn.KnownStatus2 : PBEPkmn.Status2;
            bool substitute = status2.HasFlag(PBEStatus2.Substitute);
            if (img)
            {
                UpdateImageIfRequired(substitute, status2, imgIfSubstituted);
            }

            if (visibility)
            {
                Pos.Sprite.IsVisible = ShouldBeVisible(substitute, status2);
            }

            if (color)
            {
                UpdateMaskColor(substitute, PBEPkmn.Status1);
            }
        }
        private void UpdateImageIfRequired(bool substitute, PBEStatus2 status2, bool spriteImgIfSubstituted)
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
            Pos.Sprite.UpdateImage(newImg);
            UpdateAnimationSpeed(); // Doesn't matter what speed the substitute is at since it's not animated
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
        private void UpdateMaskColor(bool substitute, PBEStatus1 status1)
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

            BattleSprite sprite = Pos.Sprite;
            if (sprite.MaskColor == color && sprite.AnimateMaskColor == animateIt)
            {
                return; // If it's already set, don't restart the animation
            }
            sprite.MaskColor = color;
            sprite.AnimateMaskColor = animateIt;
            sprite.MaskColorAmt = !animateIt && color is not null ? BattleSprite.MASK_COLOR_AMPLITUDE : 0f;
        }

        public void UpdateInfoBar()
        {
            GL gl = Display.OpenGL;
            InfoBar.UseAndViewport(gl);
            gl.ClearColor(Colors.FromRGBA(48, 48, 48, 128));
            gl.Clear(ClearBufferMask.ColorBufferBit);

            // Nickname
            GUIString.CreateAndRenderOneTimeString(PBEPkmn.KnownNickname, Font.DefaultSmall, FontColors.DefaultWhite_I, new Vec2I(2, 3));
            // Gender
            PBEGender gender = _useKnownInfo && !PBEPkmn.KnownStatus2.HasFlag(PBEStatus2.Transformed) ? PBEPkmn.KnownGender : PBEPkmn.Gender;
            GUIString.CreateAndRenderOneTimeGenderString(gender, Font.Default, new Vec2I(51, -2));
            // Level
            const int lvX = 62;
            GUIString.CreateAndRenderOneTimeString("[LV]", Font.PartyNumbers, FontColors.DefaultWhite_I, new Vec2I(lvX, 3));
            GUIString.CreateAndRenderOneTimeString(PBEPkmn.Level.ToString(), Font.PartyNumbers, FontColors.DefaultWhite_I, new Vec2I(lvX + 12, 3));
            // Caught
            if (PBEPkmn.IsWild && Game.Instance.Save.Pokedex.IsCaught(PBEPkmn.KnownSpecies))
            {
                GUIString.CreateAndRenderOneTimeString("*", Font.Default, FontColors.DefaultRed_O, new Vec2I(2, 12));
            }
            // Status
            PBEStatus1 status = PBEPkmn.Status1;
            if (status != PBEStatus1.None)
            {
                GUIString.CreateAndRenderOneTimeString(status.ToString(), Font.DefaultSmall, FontColors.DefaultWhite_I, new Vec2I(30, 13));
            }
            // HP
            if (!_useKnownInfo)
            {
                string str = PBEPkmn.HP.ToString();
                Vec2I strSize = Font.PartyNumbers.GetSize(str);
                GUIString.CreateAndRenderOneTimeString(str, Font.PartyNumbers, FontColors.DefaultWhite_I, new Vec2I(45 - strSize.X, 28));
                GUIString.CreateAndRenderOneTimeString("/" + PBEPkmn.MaxHP, Font.PartyNumbers, FontColors.DefaultWhite_I, new Vec2I(46, 28));
            }

            const int lineStartX = 9;
            const int lineW = 82;
            RenderUtils.HP_TripleLine(new Vec2I(lineStartX, 23), lineW, PBEPkmn.HPPercentage);

            // EXP
            if (!_useKnownInfo)
            {
                RenderUtils.EXP_SingleLine(new Vec2I(lineStartX, 37), lineW, PBEPkmn.EXP, PBEPkmn.Level, PBEPkmn.OriginalSpecies, PBEPkmn.RevertForm);
            }
        }

        public void Delete()
        {
            InfoBar.Delete();
            Mini.DeductReference();
        }
    }
}
