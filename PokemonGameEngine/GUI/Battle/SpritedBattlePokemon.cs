using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed class SpritedBattlePokemon
    {
        public PartyPokemon PartyPkmn { get; }
        public PBEBattlePokemon Pkmn { get; }
        public Image Mini { get; private set; }
        public AnimatedImage AnimImage { get; private set; }
        public Image InfoBarImg { get; }
        private readonly bool _backImage;
        private readonly bool _useKnownInfo;

        public SpritedBattlePokemon(PBEBattlePokemon pkmn, PartyPokemon pPkmn, bool backImage, bool useKnownInfo, PkmnPosition wildPos)
        {
            PartyPkmn = pPkmn;
            Pkmn = pkmn;
            _backImage = backImage;
            _useKnownInfo = useKnownInfo;
            InfoBarImg = new Image(100, useKnownInfo ? 30 : 42);
            UpdateInfoBar();
            UpdateSprites(wildPos, wildPos is null);
            UpdateAnimationSpeed(); // Ensure the proper speed is set upon entering battle
            if (wildPos != null)
            {
                wildPos.InfoVisible = false;
                wildPos.PkmnVisible = true;
                wildPos.SPkmn = this;
            }
        }

        // Will cause double load for some cases (like status2 updating)
        // Because new animated image is created
        public void UpdateSprites(PkmnPosition pos, bool paused)
        {
            Mini = PokemonImageUtils.GetMini(Pkmn.KnownSpecies, Pkmn.KnownForm, Pkmn.KnownGender, Pkmn.KnownShiny, PartyPkmn.IsEgg);
            PBEStatus2 status2 = _useKnownInfo ? Pkmn.KnownStatus2 : Pkmn.Status2;
            AnimImage = PokemonImageUtils.GetPokemonImage(Pkmn.KnownSpecies, Pkmn.KnownForm, Pkmn.KnownGender, Pkmn.KnownShiny, _backImage, status2.HasFlag(PBEStatus2.Substitute), PartyPkmn.PID, PartyPkmn.IsEgg);
            AnimImage.IsPaused = paused;
            if (pos is null)
            {
                return; // Only for updating visibility below
            }
            if (!status2.HasFlag(PBEStatus2.Substitute))
            {
                if (status2.HasFlag(PBEStatus2.Airborne)
                    || status2.HasFlag(PBEStatus2.ShadowForce)
                    || status2.HasFlag(PBEStatus2.Underground)
                    || status2.HasFlag(PBEStatus2.Underwater))
                {
                    pos.PkmnVisible = false;
                }
                else
                {
                    pos.PkmnVisible = true;
                }
            }
        }
        public unsafe void UpdateInfoBar()
        {
            fixed (uint* bmpAddress = InfoBarImg.Bitmap)
            {
                RenderUtils.OverwriteRectangle(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, RenderUtils.Color(48, 48, 48, 128));
                // Nickname
                Font.DefaultSmall.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, 2, 3, Pkmn.KnownNickname, Font.DefaultWhite);
                // Gender
                PBEGender gender = _useKnownInfo && !Pkmn.KnownStatus2.HasFlag(PBEStatus2.Transformed) ? Pkmn.KnownGender : Pkmn.Gender;
                if (gender != PBEGender.Genderless)
                {
                    Font.Default.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, 51, -2, gender.ToSymbol(), gender == PBEGender.Male ? Font.DefaultMale : Font.DefaultFemale);
                }
                // Level
                const int lvX = 62;
                Font.PartyNumbers.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, lvX, 3, "[LV]", Font.DefaultWhite);
                Font.PartyNumbers.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, lvX + 12, 3, Pkmn.Level.ToString(), Font.DefaultWhite);
                // Caught
                if (_useKnownInfo && Pkmn.IsWild && Game.Instance.Save.Pokedex.IsCaught(Pkmn.KnownSpecies))
                {
                    Font.Default.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, 2, 12, "*", Font.DefaultFemale);
                }
                // Status
                PBEStatus1 status = Pkmn.Status1;
                if (status != PBEStatus1.None)
                {
                    Font.DefaultSmall.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, 30, 13, status.ToString(), Font.DefaultWhite);
                }
                // HP
                if (!_useKnownInfo)
                {
                    string str = Pkmn.HP.ToString();
                    Font.PartyNumbers.MeasureString(str, out int strW, out int _);
                    Font.PartyNumbers.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, 45 - strW, 28, str, Font.DefaultWhite);
                    Font.PartyNumbers.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, 46, 28, "/" + Pkmn.MaxHP, Font.DefaultWhite);
                }

                uint hpSides, hpMid;
                if (Pkmn.HPPercentage <= 0.20)
                {
                    hpSides = RenderUtils.Color(148, 33, 49, 255);
                    hpMid = RenderUtils.Color(255, 49, 66, 255);
                }
                else if (Pkmn.HPPercentage <= 0.50)
                {
                    hpSides = RenderUtils.Color(156, 99, 16, 255);
                    hpMid = RenderUtils.Color(247, 181, 0, 255);
                }
                else
                {
                    hpSides = RenderUtils.Color(0, 140, 41, 255);
                    hpMid = RenderUtils.Color(0, 255, 74, 255);
                }
                const int lineStartX = 10;
                const int lineStartY = 24;
                const int lineW = 80;
                RenderUtils.DrawRectangle(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, lineStartX - 1, lineStartY - 1, lineW + 2, 5, RenderUtils.Color(49, 49, 49, 255));
                RenderUtils.FillRectangle(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, lineStartX, lineStartY, lineW, 3, RenderUtils.Color(33, 33, 33, 255));
                double hpp = Pkmn.HPPercentage;
                int theW = (int)(lineW * hpp);
                if (theW == 0 && hpp > 0)
                {
                    theW = 1;
                }
                RenderUtils.DrawHorizontalLine_Width(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, lineStartX, lineStartY, theW, hpSides);
                RenderUtils.DrawHorizontalLine_Width(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, lineStartX, lineStartY + 1, theW, hpMid);
                RenderUtils.DrawHorizontalLine_Width(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, lineStartX, lineStartY + 2, theW, hpSides);

                // EXP
                if (!_useKnownInfo)
                {
                    const int lineStartY2 = 38;
                    RenderUtils.DrawRectangle(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, lineStartX - 1, lineStartY2 - 1, lineW + 2, 3, RenderUtils.Color(49, 49, 49, 255));
                    RenderUtils.DrawHorizontalLine_Width(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, lineStartX, lineStartY2, lineW, RenderUtils.Color(33, 33, 33, 255));
                    double expp;
                    if (Pkmn.Level == PkmnConstants.MaxLevel)
                    {
                        expp = 0;
                    }
                    else
                    {
                        PBEGrowthRate gr = new BaseStats(Pkmn.Species, Pkmn.RevertForm).GrowthRate;
                        uint expPrev = PBEEXPTables.GetEXPRequired(gr, Pkmn.Level);
                        uint expNext = PBEEXPTables.GetEXPRequired(gr, (byte)(Pkmn.Level + 1));
                        uint expCur = Pkmn.EXP;
                        expp = (double)(expCur - expPrev) / (expNext - expPrev);
                    }
                    theW = (int)(lineW * expp);
                    if (theW == 0 && expp > 0)
                    {
                        theW = 1;
                    }
                    RenderUtils.DrawHorizontalLine_Width(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, lineStartX, lineStartY2, theW, RenderUtils.Color(0, 160, 255, 255));
                }
            }
        }

        public void UpdateAnimationSpeed()
        {
            PBEBattlePokemon pkmn = Pkmn;
            PBEStatus1 s = pkmn.Status1;
            if (s == PBEStatus1.Frozen)
            {
                AnimImage.IsPaused = true;
            }
            else
            {
                AnimImage.SpeedModifier = s == PBEStatus1.Paralyzed || s == PBEStatus1.Asleep || pkmn.HPPercentage <= 0.25 ? 2d : 1d;
                AnimImage.IsPaused = false;
            }
        }
    }

    internal sealed class SpritedBattlePokemonParty
    {
        public Party Party { get; }
        public SpritedBattlePokemon[] SpritedParty { get; }
        public PBEList<PBEBattlePokemon> BattleParty { get; }

        public SpritedBattlePokemon this[PBEBattlePokemon pkmn] => SpritedParty[pkmn.Id];

        public SpritedBattlePokemonParty(PBEList<PBEBattlePokemon> pBattle, Party p, bool backImage, bool useKnownInfo, BattleGUI battleGUI)
        {
            Party = p;
            BattleParty = pBattle;
            SpritedParty = new SpritedBattlePokemon[pBattle.Count];
            for (int i = 0; i < pBattle.Count; i++)
            {
                PkmnPosition wildPos = null;
                PBEBattlePokemon pPkmn = pBattle[i];
                if (pPkmn.IsWild)
                {
                    wildPos = battleGUI.GetStuff(pPkmn, pPkmn.FieldPosition);
                }
                SpritedParty[i] = new SpritedBattlePokemon(pBattle[i], p[i], backImage, useKnownInfo, wildPos);
            }
        }

        public void UpdateToParty()
        {
            for (int i = 0; i < Party.Count; i++)
            {
                Party[i].UpdateFromBattle(SpritedParty[i].Pkmn);
            }
        }
    }
}
