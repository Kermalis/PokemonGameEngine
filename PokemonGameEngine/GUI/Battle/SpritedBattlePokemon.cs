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
        public Sprite FrontSprite { get; }
        public Sprite BackSprite { get; }

        public SpritedBattlePokemon(PBEBattlePokemon pkmn)
        {
            Pkmn = pkmn;
            Minisprite = SpriteUtils.GetMinisprite(pkmn.OriginalSpecies, pkmn.RevertForm, pkmn.Gender, pkmn.Shiny);
            FrontSprite = SpriteUtils.GetPokemonSprite(pkmn.OriginalSpecies, pkmn.RevertForm, pkmn.Gender, pkmn.Shiny, false, false);
            BackSprite = SpriteUtils.GetPokemonSprite(pkmn.OriginalSpecies, pkmn.RevertForm, pkmn.Gender, pkmn.Shiny, true, false);
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
