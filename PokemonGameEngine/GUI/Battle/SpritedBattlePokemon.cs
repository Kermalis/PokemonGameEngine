using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed class SpritedBattlePokemon
    {
        public PartyPokemon PartyPkmn { get; }
        public PBEBattlePokemon Pkmn { get; }
        public Sprite Minisprite { get; private set; }
        public AnimatedSprite Sprite { get; private set; }
        private readonly bool _backSprite;
        private readonly bool _useKnownInfo;

        public SpritedBattlePokemon(PBEBattlePokemon pkmn, PartyPokemon pPkmn, bool backSprite, bool useKnownInfo, PkmnPosition wildPos)
        {
            PartyPkmn = pPkmn;
            Pkmn = pkmn;
            _backSprite = backSprite;
            _useKnownInfo = useKnownInfo;
            UpdateInfoBar();
            UpdateSprites(wildPos);
            UpdateAnimationSpeed(); // Ensure the proper speed is set upon entering battle
            if (wildPos != null)
            {
                wildPos.InfoVisible = false;
                wildPos.PkmnVisible = true;
                wildPos.SPkmn = this;
            }
        }

        public void UpdateSprites(PkmnPosition pos)
        {
            Minisprite = SpriteUtils.GetMinisprite(Pkmn.KnownSpecies, Pkmn.KnownForm, Pkmn.KnownGender, Pkmn.KnownShiny);
            PBEStatus2 status2 = _useKnownInfo ? Pkmn.KnownStatus2 : Pkmn.Status2;
            Sprite = SpriteUtils.GetPokemonSprite(Pkmn.KnownSpecies, Pkmn.KnownForm, Pkmn.KnownGender, Pkmn.KnownShiny, _backSprite, status2.HasFlag(PBEStatus2.Substitute), PartyPkmn.PID);
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
        public void UpdateInfoBar()
        {
            // update render hp and status etc
        }

        public void UpdateAnimationSpeed()
        {
            PBEBattlePokemon pkmn = Pkmn;
            PBEStatus1 s = pkmn.Status1;
            if (s == PBEStatus1.Frozen)
            {
                Sprite.IsPaused = true;
            }
            else
            {
                Sprite.SpeedModifier = s == PBEStatus1.Paralyzed || s == PBEStatus1.Asleep || pkmn.HPPercentage <= 0.25 ? 2d : 1d;
                Sprite.IsPaused = false;
            }
        }
    }

    internal sealed class SpritedBattlePokemonParty
    {
        public Party Party { get; }
        public SpritedBattlePokemon[] SpritedParty { get; }
        public PBEList<PBEBattlePokemon> BattleParty { get; }

        public SpritedBattlePokemon this[PBEBattlePokemon pkmn] => SpritedParty[pkmn.Id];

        public SpritedBattlePokemonParty(PBEList<PBEBattlePokemon> pBattle, Party p, bool backSprite, bool useKnownInfo, BattleGUI battleGUI)
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
                SpritedParty[i] = new SpritedBattlePokemon(pBattle[i], p[i], backSprite, useKnownInfo, wildPos);
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
