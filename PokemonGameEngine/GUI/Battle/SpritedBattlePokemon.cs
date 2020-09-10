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
        public Sprite Minisprite { get; }
        public AnimatedSprite FrontSprite { get; } // These are being animated even if unused in the battle
        public AnimatedSprite BackSprite { get; }

        public SpritedBattlePokemon(PBEBattlePokemon pkmn, PartyPokemon pPkmn)
        {
            PartyPkmn = pPkmn;
            Pkmn = pkmn;
            Minisprite = SpriteUtils.GetMinisprite(pkmn.OriginalSpecies, pkmn.RevertForm, pkmn.Gender, pkmn.Shiny);
            FrontSprite = SpriteUtils.GetPokemonSprite(pkmn.OriginalSpecies, pkmn.RevertForm, pkmn.Gender, pkmn.Shiny, false, false, pPkmn.PID);
            BackSprite = SpriteUtils.GetPokemonSprite(pkmn.OriginalSpecies, pkmn.RevertForm, pkmn.Gender, pkmn.Shiny, true, false, pPkmn.PID);

            UpdateAnimationSpeed();
        }

        public void UpdateAnimationSpeed()
        {
            PBEBattlePokemon pkmn = Pkmn;
            PBEStatus1 s = pkmn.Status1;
            if (s == PBEStatus1.Frozen)
            {
                BackSprite.IsPaused = true;
                FrontSprite.IsPaused = true;
            }
            else
            {
                double speed = s == PBEStatus1.Paralyzed || s == PBEStatus1.Asleep || pkmn.HPPercentage <= 0.25 ? 2d : 1d;
                BackSprite.SpeedModifier = speed;
                FrontSprite.SpeedModifier = speed;
                BackSprite.IsPaused = false;
                FrontSprite.IsPaused = false;
            }
        }
    }

    internal sealed class SpritedBattlePokemonParty
    {
        public Party Party { get; }
        public SpritedBattlePokemon[] SpritedParty { get; }
        public PBEList<PBEBattlePokemon> BattleParty { get; }

        public SpritedBattlePokemonParty(PBEList<PBEBattlePokemon> pBattle, Party p)
        {
            Party = p;
            BattleParty = pBattle;
            SpritedParty = new SpritedBattlePokemon[pBattle.Count];
            for (int i = 0; i < pBattle.Count; i++)
            {
                SpritedParty[i] = new SpritedBattlePokemon(pBattle[i], p[i]);
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
