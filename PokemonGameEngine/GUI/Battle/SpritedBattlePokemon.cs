using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed class SpritedBattlePokemon
    {
        public PBEBattlePokemon Pkmn { get; }
        public Sprite Minisprite { get; }
        public AnimatedSprite FrontSprite { get; } // These are being animated even if unused in the battle
        public AnimatedSprite BackSprite { get; }

        public SpritedBattlePokemon(PBEBattlePokemon pkmn)
        {
            Pkmn = pkmn;
            Minisprite = SpriteUtils.GetMinisprite(pkmn.OriginalSpecies, pkmn.RevertForm, pkmn.Gender, pkmn.Shiny);
            FrontSprite = SpriteUtils.GetPokemonSprite(pkmn.OriginalSpecies, pkmn.RevertForm, pkmn.Gender, pkmn.Shiny, false, false);
            BackSprite = SpriteUtils.GetPokemonSprite(pkmn.OriginalSpecies, pkmn.RevertForm, pkmn.Gender, pkmn.Shiny, true, false);

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
                double speed = s == PBEStatus1.Paralyzed || pkmn.HPPercentage <= 0.25 ? 2d : 1d;
                BackSprite.SpeedModifier = speed;
                FrontSprite.SpeedModifier = speed;
                BackSprite.IsPaused = false;
                FrontSprite.IsPaused = false;
            }
        }
    }

    internal sealed class SpritedBattlePokemonParty
    {
        public PBEList<PBEBattlePokemon> OriginalParty { get; }
        public SpritedBattlePokemon[] SpritedParty { get; }

        public SpritedBattlePokemonParty(PBEList<PBEBattlePokemon> party)
        {
            OriginalParty = party;
            SpritedParty = new SpritedBattlePokemon[party.Count];
            for (int i = 0; i < party.Count; i++)
            {
                SpritedParty[i] = new SpritedBattlePokemon(party[i]);
            }
        }
    }
}
