using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed class SpritedBattlePokemon
    {
        public PartyPokemon PartyPkmn { get; }
        public PBEBattlePokemon Pkmn { get; }
        public uint DisguisedPID { get; set; }
        public Image Mini { get; private set; }
        public WriteableImage InfoBarImg { get; }
        private readonly bool _backImage;
        private readonly bool _useKnownInfo;

        public SpritedBattlePokemon(PBEBattlePokemon pkmn, PartyPokemon pPkmn, bool backImage, bool useKnownInfo, PkmnPosition wildPos = null)
        {
            PartyPkmn = pPkmn;
            Pkmn = pkmn;
            DisguisedPID = pPkmn.PID; // By default, use our own PID (for example, wild disguised pkmn)
            _backImage = backImage;
            _useKnownInfo = useKnownInfo;
            InfoBarImg = new WriteableImage(new Size2D(100, useKnownInfo ? 30u : 42));
            UpdateInfoBar();
            if (wildPos is not null)
            {
                UpdateSprites(wildPos, true, true, true, true);
                wildPos.InfoVisible = false;
                wildPos.SPkmn = this;
                wildPos.UpdateAnimationSpeed(pkmn); // Ensure the proper speed is set upon entering battle
            }
            else
            {
                UpdateMini();
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

        private void UpdateMini()
        {
            Mini?.DeductReference();
            Mini = PokemonImageLoader.GetMini(Pkmn.KnownSpecies, Pkmn.KnownForm, Pkmn.KnownGender, Pkmn.KnownShiny, PartyPkmn.IsEgg);
        }
        // TODO: Make substitute imgs separate
        public void UpdateSprites(PkmnPosition pos, bool updateSprite, bool updateSpriteIfSubstituted, bool updateMini, bool updateVisibility)
        {
            if (updateMini)
            {
                UpdateMini();
            }
            PBEStatus2 status2 = _useKnownInfo ? Pkmn.KnownStatus2 : Pkmn.Status2;
            bool substitute = status2.HasFlag(PBEStatus2.Substitute);
            // If behind a sub, update if requested
            // If not behind a sub, update always
            if (updateSprite && (!substitute || updateSpriteIfSubstituted))
            {
                Sprite sprite = pos.Sprite;
                sprite.Image?.DeductReference();
                sprite.Image = PokemonImageLoader.GetPokemonImage(Pkmn.KnownSpecies, Pkmn.KnownForm, Pkmn.KnownGender, Pkmn.KnownShiny, _backImage,
                    substitute, status2.HasFlag(PBEStatus2.Disguised) ? DisguisedPID : PartyPkmn.PID, PartyPkmn.IsEgg);
            }
            if (!updateVisibility)
            {
                return;
            }
            if (!substitute &&
                (status2.HasFlag(PBEStatus2.Airborne)
                || status2.HasFlag(PBEStatus2.ShadowForce)
                || status2.HasFlag(PBEStatus2.Underground)
                || status2.HasFlag(PBEStatus2.Underwater)))
            {
                pos.PkmnVisible = false;
            }
            else
            {
                pos.PkmnVisible = true;
            }
        }
        public void UpdateInfoBar()
        {
            GL gl = Display.OpenGL;
            InfoBarImg.PushFrameBuffer(gl);
            GLHelper.ClearColor(gl, Colors.FromRGBA(48, 48, 48, 128));
            gl.Clear(ClearBufferMask.ColorBufferBit);

            // Nickname
            GUIString.CreateAndRenderOneTimeString(Pkmn.KnownNickname, Font.DefaultSmall, FontColors.DefaultWhite_I, new Pos2D(2, 3));
            // Gender
            PBEGender gender = _useKnownInfo && !Pkmn.KnownStatus2.HasFlag(PBEStatus2.Transformed) ? Pkmn.KnownGender : Pkmn.Gender;
            GUIString.CreateAndRenderOneTimeGenderString(gender, Font.Default, new Pos2D(51, -2));
            // Level
            const int lvX = 62;
            GUIString.CreateAndRenderOneTimeString("[LV]", Font.PartyNumbers, FontColors.DefaultWhite_I, new Pos2D(lvX, 3));
            GUIString.CreateAndRenderOneTimeString(Pkmn.Level.ToString(), Font.PartyNumbers, FontColors.DefaultWhite_I, new Pos2D(lvX + 12, 3));
            // Caught
            if (_useKnownInfo && Pkmn.IsWild && Game.Instance.Save.Pokedex.IsCaught(Pkmn.KnownSpecies))
            {
                GUIString.CreateAndRenderOneTimeString("*", Font.Default, FontColors.DefaultRed_O, new Pos2D(2, 12));
            }
            // Status
            PBEStatus1 status = Pkmn.Status1;
            if (status != PBEStatus1.None)
            {
                GUIString.CreateAndRenderOneTimeString(status.ToString(), Font.DefaultSmall, FontColors.DefaultWhite_I, new Pos2D(30, 13));
            }
            // HP
            if (!_useKnownInfo)
            {
                string str = Pkmn.HP.ToString();
                Size2D strS = Font.PartyNumbers.MeasureString(str);
                GUIString.CreateAndRenderOneTimeString(str, Font.PartyNumbers, FontColors.DefaultWhite_I, new Pos2D(45 - (int)strS.Width, 28));
                GUIString.CreateAndRenderOneTimeString("/" + Pkmn.MaxHP, Font.PartyNumbers, FontColors.DefaultWhite_I, new Pos2D(46, 28));
            }

            const int lineStartX = 9;
            const int lineW = 82;
            Renderer.HP_TripleLine(lineStartX, 23, lineW, Pkmn.HPPercentage);

            // EXP
            if (!_useKnownInfo)
            {
                Renderer.EXP_SingleLine(lineStartX, 37, lineW, Pkmn.EXP, Pkmn.Level, Pkmn.Species, Pkmn.RevertForm);
            }


            GLHelper.PopFrameBuffer(gl);
        }

        public void Delete()
        {
            InfoBarImg.DeductReference();
            Mini?.DeductReference();
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
                    wildPos = battleGUI.GetPkmnPosition(pPkmn, pPkmn.FieldPosition);
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
                    if (evo is not null)
                    {
                        Evolution.AddPendingEvolution(pp, evo);
                    }
                }
            }
        }
    }
}
