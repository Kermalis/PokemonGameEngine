using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Scripts;
using System;
using System.Collections.Generic;

// If you are going to add script commands, you need to edit the below dictionary to define their arguments for the script builder
// A command with a variable amount of arguments would need extra work, so that's your problem lmao [or just make it into multiple commands like I did with GivePokemon :)]
internal static class ScriptBuilderHelper
{
    public const string VarPrefix = "Var.";
    public static readonly Dictionary<Type, string> EnumDefines = new Dictionary<Type, string>()
    {
        { typeof(Flag), "Flag." },
        { typeof(Var), VarPrefix },
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
        { ScriptCommand.GoTo, new[] { typeof(void*) } }, // Offset to go to
        { ScriptCommand.Call, new[] { typeof(void*) } }, // Offset to jump to
        { ScriptCommand.Return, Array.Empty<Type>() },
        { ScriptCommand.HealParty, Array.Empty<Type>() },
        { ScriptCommand.GivePokemon, new[] { typeof(PBESpecies), typeof(byte) } }, // Species, level
        { ScriptCommand.GivePokemonForm, new[] { typeof(PBESpecies), typeof(PBEForm), typeof(byte) } }, // Species, form, level
        { ScriptCommand.GivePokemonFormItem, new[] { typeof(PBESpecies), typeof(PBEForm), typeof(byte), typeof(PBEItem) } }, // Species, form, level, item
        { ScriptCommand.MoveObj, new[] { typeof(ushort), typeof(void*) } }, // Id, movement data offset
        { ScriptCommand.AwaitObjMovement, new[] { typeof(ushort) } }, // Id
        { ScriptCommand.DetachCamera, Array.Empty<Type>() },
        { ScriptCommand.AttachCamera, new[] { typeof(ushort) } }, // Id
        { ScriptCommand.Delay, new[] { typeof(ushort) } }, // Delay
        { ScriptCommand.SetFlag, new[] { typeof(Flag) } }, // Flag
        { ScriptCommand.ClearFlag, new[] { typeof(Flag) } }, // Flag
        { ScriptCommand.Warp, new[] { typeof(string), typeof(int), typeof(int), typeof(byte) } }, // Map id, x, y, elevation
        { ScriptCommand.Message, new[] { typeof(void*) } }, // String data offset
        { ScriptCommand.AwaitMessage, Array.Empty<Type>() },
        { ScriptCommand.LockObj, new[] { typeof(ushort) } }, // Id
        { ScriptCommand.UnlockObj, new[] { typeof(ushort) } }, // Id
        { ScriptCommand.LockAllObjs, Array.Empty<Type>() },
        { ScriptCommand.UnlockAllObjs, Array.Empty<Type>() },
        { ScriptCommand.SetVar, new[] { typeof(Var), typeof(ushort) } }, // Var, value
        { ScriptCommand.AddVar, new[] { typeof(Var), typeof(ushort) } }, // Var, value
        { ScriptCommand.SubVar, new[] { typeof(Var), typeof(ushort) } }, // Var, value
        { ScriptCommand.MulVar, new[] { typeof(Var), typeof(ushort) } }, // Var, value
        { ScriptCommand.DivVar, new[] { typeof(Var), typeof(ushort) } }, // Var, value
        { ScriptCommand.RshftVar, new[] { typeof(Var), typeof(ushort) } }, // Var, value
        { ScriptCommand.LshiftVar, new[] { typeof(Var), typeof(ushort) } }, // Var, value
        { ScriptCommand.AndVar, new[] { typeof(Var), typeof(ushort) } }, // Var, value
        { ScriptCommand.OrVar, new[] { typeof(Var), typeof(ushort) } }, // Var, value
        { ScriptCommand.XorVar, new[] { typeof(Var), typeof(ushort) } }, // Var, value
        { ScriptCommand.RandomizeVar, new[] { typeof(Var), typeof(ushort), typeof(ushort) } }, // Var, minValue, maxValue
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
