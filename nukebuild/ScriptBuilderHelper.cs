using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Player;
using Kermalis.PokemonGameEngine.Scripts;
using System;
using System.Collections.Generic;

// If you are going to add script commands, you need to edit the below dictionary to define their arguments for the script builder
// A command with a variable amount of arguments would need extra work, so that's your problem lmao [or just make it into multiple commands like I did with GivePokemon :)]
internal static class ScriptBuilderHelper
{
    public static readonly Dictionary<Type, string> EnumDefines = new()
    {
        { typeof(DaycareState), "DaycareState." },
        { typeof(Flag), "Flag." },
        { typeof(GameStat), "GameStat." },
        { typeof(ScriptConditional), "C." },
        { typeof(Var), "Var." },
        { typeof(PBEForm), "Form." },
        { typeof(PBEItem), "Item." },
        { typeof(PBEMove), "Move." },
        { typeof(PBESpecies), "Species." }
    };
    public static readonly Dictionary<string, IdList> StringDefines = new()
    {
        { "Map.", new IdList(Build.AssetPath / "Map" / "MapIds.txt") }
    };
    public static readonly (string OldChars, string NewChars)[] TextReplacements = new[]
    {
        ("\\f", "\f"),
        ("\\n", "\n"),
        ("\\v", "\v"),
    };

    public static readonly ScriptCommand[] Commands = Enum.GetValues<ScriptCommand>();
    public static readonly Dictionary<ScriptCommand, Type[]> CommandArgs = new()
    {
        { ScriptCommand.End, Array.Empty<Type>() },
        { ScriptCommand.GoTo, new[] { typeof(void*) } }, // Offset to go to
        { ScriptCommand.Call, new[] { typeof(void*) } }, // Offset to jump to
        { ScriptCommand.Return, Array.Empty<Type>() },
        { ScriptCommand.HealParty, Array.Empty<Type>() },
        { ScriptCommand.GivePokemon, new[] { typeof(PBESpecies), typeof(byte) } }, // Species, level
        { ScriptCommand.GivePokemonForm, new[] { typeof(PBESpecies), typeof(PBEForm), typeof(byte) } }, // Species, form, level
        { ScriptCommand.GivePokemonFormItem, new[] { typeof(PBESpecies), typeof(PBEForm), typeof(byte), typeof(ItemType) } }, // Species, form, level, item
        { ScriptCommand.MoveObj, new[] { typeof(ushort), typeof(void*) } }, // Id, movement data offset
        { ScriptCommand.AwaitObjMovement, new[] { typeof(ushort) } }, // Id
        { ScriptCommand.DetachCamera, Array.Empty<Type>() },
        { ScriptCommand.AttachCamera, new[] { typeof(ushort) } }, // Id
        { ScriptCommand.Delay, new[] { typeof(float) } }, // Delay in seconds
        { ScriptCommand.SetFlag, new[] { typeof(Flag) } }, // Flag
        { ScriptCommand.ClearFlag, new[] { typeof(Flag) } }, // Flag
        { ScriptCommand.Warp, new[] { typeof(string), typeof(int), typeof(int), typeof(byte) } }, // Map id, x, y, elevation
        { ScriptCommand.Message, new[] { typeof(void*) } }, // String data offset
        { ScriptCommand.AwaitMessageRead, Array.Empty<Type>() },
        { ScriptCommand.AwaitMessageComplete, Array.Empty<Type>() },
        { ScriptCommand.LockObj, new[] { typeof(ushort) } }, // Id
        { ScriptCommand.UnlockObj, new[] { typeof(ushort) } }, // Id
        { ScriptCommand.LockAllObjs, Array.Empty<Type>() },
        { ScriptCommand.UnlockAllObjs, Array.Empty<Type>() },
        { ScriptCommand.SetVar, new[] { typeof(Var), typeof(short) } }, // Var, value
        { ScriptCommand.AddVar, new[] { typeof(Var), typeof(short) } }, // Var, value
        { ScriptCommand.SubVar, new[] { typeof(Var), typeof(short) } }, // Var, value
        { ScriptCommand.MulVar, new[] { typeof(Var), typeof(short) } }, // Var, value
        { ScriptCommand.DivVar, new[] { typeof(Var), typeof(short) } }, // Var, value
        { ScriptCommand.RshftVar, new[] { typeof(Var), typeof(short) } }, // Var, value
        { ScriptCommand.LshiftVar, new[] { typeof(Var), typeof(short) } }, // Var, value
        { ScriptCommand.AndVar, new[] { typeof(Var), typeof(short) } }, // Var, value
        { ScriptCommand.OrVar, new[] { typeof(Var), typeof(short) } }, // Var, value
        { ScriptCommand.XorVar, new[] { typeof(Var), typeof(short) } }, // Var, value
        { ScriptCommand.RandomizeVar, new[] { typeof(Var), typeof(short), typeof(short) } }, // Var, minValue, maxValue
        { ScriptCommand.GoToIf, new[] { typeof(void*), typeof(short), typeof(ScriptConditional), typeof(short) } }, // Offset to go to, value1, condition, value2
        { ScriptCommand.GoToIfFlag, new[] { typeof(void*), typeof(Flag), typeof(byte) } }, // Offset to go to, flag, value
        { ScriptCommand.CallIf, new[] { typeof(void*), typeof(short), typeof(ScriptConditional), typeof(short) } }, // Offset to jump to, value1, condition, value2
        { ScriptCommand.CallIfFlag, new[] { typeof(void*), typeof(Flag), typeof(byte) } }, // Offset to jump to, flag, value
        { ScriptCommand.BufferSpeciesName, new[] { typeof(byte), typeof(PBESpecies) } }, // Buffer number, species
        { ScriptCommand.BufferPartyMonNickname, new[] { typeof(byte), typeof(byte) } }, // Buffer number, party index
        { ScriptCommand.WildBattle, new[] { typeof(PBESpecies), typeof(PBEForm), typeof(byte) } }, // Species, form, level
        { ScriptCommand.TrainerBattle, new[] { typeof(Flag), typeof(void*), typeof(void*) } }, // Trainer, intro text, defeat text
        { ScriptCommand.TrainerBattle_Continue, new[] { typeof(Flag), typeof(void*), typeof(void*), typeof(void*) } }, // Trainer, intro text, defeat text, continue script
        { ScriptCommand.AwaitReturnToField, Array.Empty<Type>() },
        { ScriptCommand.CloseMessage, Array.Empty<Type>() },
        { ScriptCommand.UnloadObj, new[] { typeof(ushort) } }, // Id
        { ScriptCommand.LookTowardsObj, new[] { typeof(ushort), typeof(ushort) } }, // Id of looker, id of obj to look at
        { ScriptCommand.LookLastTalkedTowardsPlayer, Array.Empty<Type>() },
        { ScriptCommand.BufferSeenCount, new[] { typeof(byte) } }, // Buffer number
        { ScriptCommand.BufferCaughtCount, new[] { typeof(byte) } }, // Buffer number
        { ScriptCommand.GetDaycareState, Array.Empty<Type>() },
        { ScriptCommand.BufferDaycareMonNickname, new[] { typeof(byte), typeof(byte) } }, // Buffer number, Daycare index
        { ScriptCommand.StorePokemonInDaycare, Array.Empty<Type>() },
        { ScriptCommand.GetDaycareCompatibility, Array.Empty<Type>() },
        { ScriptCommand.SelectDaycareMon, Array.Empty<Type>() },
        { ScriptCommand.GetDaycareMonLevelsGained, new[] { typeof(byte) } }, // Daycare index number
        { ScriptCommand.GiveDaycareEgg, Array.Empty<Type>() },
        { ScriptCommand.DisposeDaycareEgg, Array.Empty<Type>() },
        { ScriptCommand.HatchEgg, Array.Empty<Type>() },
        { ScriptCommand.YesNoChoice, Array.Empty<Type>() },
        { ScriptCommand.IncrementGameStat, new[] { typeof(GameStat) } }, // Game stat
        { ScriptCommand.PlayCry, new[] { typeof(PBESpecies), typeof(PBEForm) } }, // Species, form
        { ScriptCommand.AwaitCry, Array.Empty<Type>() },
        { ScriptCommand.CountNonEggParty, Array.Empty<Type>() },
        { ScriptCommand.CountNonFaintedNonEggParty, Array.Empty<Type>() },
        { ScriptCommand.CountPlayerParty, Array.Empty<Type>() },
        { ScriptCommand.CountBadges, Array.Empty<Type>() },
        { ScriptCommand.BufferBadges, new[] { typeof(byte) } }, // Buffer number
        { ScriptCommand.CheckPartyHasMove, new[] { typeof(PBEMove) } }, // Move
        { ScriptCommand.UseSurf, Array.Empty<Type>() },
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
