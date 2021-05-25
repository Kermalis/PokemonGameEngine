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
        public uint DisguisedPID { get; set; }
        public Image Mini { get; private set; }
        public AnimatedImage AnimImage { get; private set; }
        public Image InfoBarImg { get; }
        private readonly bool _backImage;
        private readonly bool _useKnownInfo;

        public SpritedBattlePokemon(PBEBattlePokemon pkmn, PartyPokemon pPkmn, bool backImage, bool useKnownInfo, PkmnPosition wildPos)
        {
            PartyPkmn = pPkmn;
            Pkmn = pkmn;
            DisguisedPID = pPkmn.PID; // By default, use our own PID (for example, wild disguised pkmn)
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

        public void UpdateDisguisedPID(SpritedBattlePokemonParty sParty)
        {
            if (Pkmn.Status2.HasFlag(PBEStatus2.Disguised))
            {
                PBEBattlePokemon p = Pkmn.GetPkmnWouldDisguiseAs();
                DisguisedPID = p is null ? PartyPkmn.PID : sParty[p].PartyPkmn.PID;
            }
            else
            {
                DisguisedPID = PartyPkmn.PID; // Set back to normal
            }
        }

        // Will cause double load for some cases (like status2 updating)
        // Because new animated image is created
        public void UpdateSprites(PkmnPosition pos, bool paused)
        {
            Mini = PokemonImageUtils.GetMini(Pkmn.KnownSpecies, Pkmn.KnownForm, Pkmn.KnownGender, Pkmn.KnownShiny, PartyPkmn.IsEgg);
            PBEStatus2 status2 = _useKnownInfo ? Pkmn.KnownStatus2 : Pkmn.Status2;
            AnimImage = PokemonImageUtils.GetPokemonImage(Pkmn.KnownSpecies, Pkmn.KnownForm, Pkmn.KnownGender, Pkmn.KnownShiny, _backImage,
                status2.HasFlag(PBEStatus2.Substitute), status2.HasFlag(PBEStatus2.Disguised) ? DisguisedPID : PartyPkmn.PID, PartyPkmn.IsEgg);
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
                Font.DefaultSmall.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, 2, 3, Pkmn.KnownNickname, Font.DefaultWhite_I);
                // Gender
                PBEGender gender = _useKnownInfo && !Pkmn.KnownStatus2.HasFlag(PBEStatus2.Transformed) ? Pkmn.KnownGender : Pkmn.Gender;
                if (gender != PBEGender.Genderless)
                {
                    Font.Default.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, 51, -2, gender.ToSymbol(), gender == PBEGender.Male ? Font.DefaultBlue_O : Font.DefaultRed_O);
                }
                // Level
                const int lvX = 62;
                Font.PartyNumbers.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, lvX, 3, "[LV]", Font.DefaultWhite_I);
                Font.PartyNumbers.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, lvX + 12, 3, Pkmn.Level.ToString(), Font.DefaultWhite_I);
                // Caught
                if (_useKnownInfo && Pkmn.IsWild && Game.Instance.Save.Pokedex.IsCaught(Pkmn.KnownSpecies))
                {
                    Font.Default.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, 2, 12, "*", Font.DefaultRed_O);
                }
                // Status
                PBEStatus1 status = Pkmn.Status1;
                if (status != PBEStatus1.None)
                {
                    Font.DefaultSmall.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, 30, 13, status.ToString(), Font.DefaultWhite_I);
                }
                // HP
                if (!_useKnownInfo)
                {
                    string str = Pkmn.HP.ToString();
                    Font.PartyNumbers.MeasureString(str, out int strW, out int _);
                    Font.PartyNumbers.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, 45 - strW, 28, str, Font.DefaultWhite_I);
                    Font.PartyNumbers.DrawString(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, 46, 28, "/" + Pkmn.MaxHP, Font.DefaultWhite_I);
                }

                const int lineStartX = 9;
                const int lineW = 82;
                RenderUtils.HP_TripleLine(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, lineStartX, 23, lineW, Pkmn.HPPercentage);

                // EXP
                if (!_useKnownInfo)
                {
                    RenderUtils.EXP_SingleLine(bmpAddress, InfoBarImg.Width, InfoBarImg.Height, lineStartX, 37, lineW, Pkmn.EXP, Pkmn.Level, Pkmn.Species, Pkmn.RevertForm);
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

        public void UpdateToParty(bool shouldCheckEvolution)
        {
            for (int i = 0; i < Party.Count; i++)
            {
                PartyPokemon pp = Party[i];
                byte oldLevel = pp.Level;
                pp.UpdateFromBattle(SpritedParty[i].Pkmn);
                if (shouldCheckEvolution && oldLevel != pp.Level)
                {
                    EvolutionData.EvoData evo = Evolution.GetLevelUpEvolution(Party, pp);
                    if (evo != null)
                    {
                        Evolution.AddPendingEvolution(pp, evo);
                    }
                }
            }
        }
    }
}
