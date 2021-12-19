﻿using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed class BattlePokemonParty
    {
        public Party GameParty { get; }
        public BattlePokemon[] BattleParty { get; }
        public PBEList<PBEBattlePokemon> PBEParty { get; }

        public BattlePokemon this[PBEBattlePokemon pkmn] => BattleParty[pkmn.Id];

        public BattlePokemonParty(PBEList<PBEBattlePokemon> pbeParty, Party party, bool backImage, bool useKnownInfo, BattleGUI battleGUI)
        {
            GameParty = party;
            PBEParty = pbeParty;
            BattleParty = new BattlePokemon[pbeParty.Count];
            for (int i = 0; i < pbeParty.Count; i++)
            {
                PBEBattlePokemon pbePkmn = pbeParty[i];
                PartyPokemon pPkmn = party[i];
                if (pbePkmn.IsWild)
                {
                    PkmnPosition wildPos = battleGUI.GetPkmnPosition(pbePkmn, pbePkmn.FieldPosition);
                    BattleParty[i] = BattlePokemon.CreateForWildMon(pbePkmn, pPkmn, backImage, useKnownInfo, wildPos);
                }
                else
                {
                    BattleParty[i] = BattlePokemon.CreateForTrainerMon(pbePkmn, pPkmn, backImage, useKnownInfo);
                }
            }
        }

        public void UpdateToParty(bool shouldCheckEvolution)
        {
            for (int i = 0; i < GameParty.Count; i++)
            {
                PartyPokemon pp = GameParty[i];
                byte oldLevel = pp.Level;
                pp.UpdateFromBattle(BattleParty[i].PBEPkmn);
                if (shouldCheckEvolution && oldLevel != pp.Level)
                {
                    EvolutionData.EvoData evo = Evolution.GetLevelUpEvolution(GameParty, pp);
                    if (evo is not null)
                    {
                        Evolution.AddPendingEvolution(pp, evo);
                    }
                }
            }
        }
    }
}
