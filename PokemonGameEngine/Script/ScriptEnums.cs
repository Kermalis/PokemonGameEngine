using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Scripts
{
    public enum ScriptMovement : byte
    {
        End,
        Walk_S,
        Walk_N,
        Walk_W,
        Walk_E,
        Walk_SW,
        Walk_SE,
        Walk_NW,
        Walk_NE
    }

    public enum ScriptCommand : ushort
    {
        End,
        GoTo,
        Call,
        Return,
        HealParty,
        GivePokemon,
        GivePokemonForm,
        GivePokemonFormItem,
        MoveObj
    }

    // If you are going to add script commands, you need to edit the below dictionary to define their arguments for the script builder
    // A command with a variable amount of arguments would need extra work, so that's your problem lmao [or just make it into multiple commands like I did with GivePokemon :)]
    public static class ScriptBuilderHelper
    {
        public static readonly Array Commands = Enum.GetValues(typeof(ScriptCommand));
        public static readonly Dictionary<ScriptCommand, Type[]> CommandArgs = new Dictionary<ScriptCommand, Type[]>
        {
            { ScriptCommand.End, Array.Empty<Type>() },
            { ScriptCommand.GoTo, new[] { typeof(void*) } },
            { ScriptCommand.Call, new[] { typeof(void*) } },
            { ScriptCommand.Return, Array.Empty<Type>() },
            { ScriptCommand.HealParty, Array.Empty<Type>() },
            { ScriptCommand.GivePokemon, new[] { typeof(PBESpecies), typeof(byte) } },
            { ScriptCommand.GivePokemonForm, new[] { typeof(PBESpecies), typeof(PBEForm), typeof(byte) } },
            { ScriptCommand.GivePokemonFormItem, new[] { typeof(PBESpecies), typeof(PBEForm), typeof(byte), typeof(PBEItem) } },
            { ScriptCommand.MoveObj, new[] { typeof(ushort), typeof(void*) } }
        };

        static ScriptBuilderHelper()
        {
            foreach (ScriptCommand cmd in Commands)
            {
                if (!CommandArgs.ContainsKey(cmd))
                {
                    throw new Exception($"{cmd} does not have arguments defined");
                }
            }
        }
    }
}
