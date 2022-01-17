﻿using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kermalis.PokemonGameEngine.Pkmn.Pokedata
{
    internal sealed class LevelUpData
    {
        public readonly (PBEMove Move, byte Level)[] Moves;

        public LevelUpData(PBESpecies species, PBEForm form)
        {
            string asset = @"Pokedata\" + AssetLoader.GetPkmnDirectoryName(species, form) + @"\LevelUp.bin";
            using (var r = new EndianBinaryReader(File.OpenRead(AssetLoader.GetPath(asset))))
            {
                byte count = r.ReadByte();
                Moves = new (PBEMove, byte)[count];
                for (int i = 0; i < count; i++)
                {
                    Moves[i] = (r.ReadEnum<PBEMove>(), r.ReadByte());
                }
            }
        }

        public bool CanLearnMoveEventually(PBEMove move)
        {
            foreach ((PBEMove m, byte _) in Moves)
            {
                if (m == move)
                {
                    return true;
                }
            }
            return false;
        }

        ///<summary>Get last 4 moves that can be learned by level up, with no repeats (such as Sketch)</summary>
        public PBEMove[] GetDefaultMoves(byte level)
        {
            return Array.FindAll(Moves, t => t.Level <= level && PBEDataUtils.IsMoveUsable(t.Move))
                .Select(t => t.Move).Distinct().Reverse().Take(PkmnConstants.NumMoves).ToArray();
        }

        public IEnumerable<PBEMove> GetNewMoves(byte level)
        {
            return Array.FindAll(Moves, t => t.Level == level && PBEDataUtils.IsMoveUsable(t.Move))
                .Select(t => t.Move).Distinct();
        }
    }
}
