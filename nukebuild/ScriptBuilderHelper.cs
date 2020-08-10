using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Scripts;
using System;
using System.Collections.Generic;

// If you are going to add script commands, you need to edit the below dictionary to define their arguments for the script builder
// A command with a variable amount of arguments would need extra work, so that's your problem lmao [or just make it into multiple commands like I did with GivePokemon :)]
internal static class ScriptBuilderHelper
{
    public static readonly Dictionary<Type, string> EnumDefines = new Dictionary<Type, string>()
    {
        { typeof(Flag), "Flag." },
        { typeof(PBEForm), "Form." },
        { typeof(PBEItem), "Item." },
        { typeof(PBESpecies), "Species." }
    };
    public static readonly Dictionary<string, IdList> StringDefines = new Dictionary<string, IdList>()
    {
        { "Map.", new IdList(Build.AssetPath / "Map" / "MapIds.txt") }
    };
    public static readonly (string OldChars, string NewChars)[] TextReplacements = new (string OldChars, string NewChars)[]
    {
        ("\\n", "\n")
    };

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
        { ScriptCommand.MoveObj, new[] { typeof(ushort), typeof(void*) } },
        { ScriptCommand.AwaitObjMovement, new[] { typeof(ushort) } },
        { ScriptCommand.DetachCamera, Array.Empty<Type>() },
        { ScriptCommand.AttachCamera, new[] { typeof(ushort) } },
        { ScriptCommand.Delay, new[] { typeof(ushort) } },
        { ScriptCommand.SetFlag, new[] { typeof(Flag) } },
        { ScriptCommand.ClearFlag, new[] { typeof(Flag) } },
        { ScriptCommand.Warp, new[] { typeof(string), typeof(int), typeof(int), typeof(byte) } },
        { ScriptCommand.Message, new[] { typeof(void*) } },
        { ScriptCommand.AwaitMessage, Array.Empty<Type>() },
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
