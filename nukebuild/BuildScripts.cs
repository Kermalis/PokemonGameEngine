using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Scripts;
using Nuke.Common.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public sealed partial class Build
{
    private class Pair
    {
        public bool Global;
        public long Offset;
    }
    private class Pointer
    {
        public string Label;
        public long Offset;
    }

    private static readonly AbsolutePath ScriptPath = AssetPath / "Script";
    private static readonly AbsolutePath ScriptOutputPath = ScriptPath / "Scripts.bin";
    private const string DefineChars = ".set";
    private const string CommentChars = "//";
    private const string LocalLabelChars = "#";
    private const string GlobalLabelChars = "@";
    private const string MovementPrefix = "M.";
    private const string TextChars = "\"";

    private readonly Dictionary<string, Pair> _labels = new();
    private readonly List<Pointer> _pointers = new();
    private EndianBinaryWriter _writer;

    private void CleanScripts()
    {
        if (File.Exists(ScriptOutputPath))
        {
            File.Delete(ScriptOutputPath);
        }
    }
    private void BuildScripts()
    {
        using (var ms = new MemoryStream())
        using (_writer = new EndianBinaryWriter(ms, encoding: EncodingType.UTF16))
        {
            ParseDir(ScriptPath);
            using (var fw = new EndianBinaryWriter(File.Create(ScriptOutputPath), encoding: EncodingType.UTF16))
            {
                // Compute start offset of script data
                int numGlobals = 0;
                uint dataStart = sizeof(int); // numGlobals needs to be accounted for in offset calc
                foreach (KeyValuePair<string, Pair> kvp in _labels)
                {
                    Pair p = kvp.Value;
                    if (p.Global)
                    {
                        string label = kvp.Key;
                        numGlobals++;
                        dataStart += (uint)((label.Length * 2) + 2 + sizeof(uint)); // 2 bytes per char, 2 bytes for nullTermination, sizeof for the pointer
                    }
                }
                // Write global label table
                fw.Write(numGlobals);
                foreach (KeyValuePair<string, Pair> kvp in _labels)
                {
                    Pair p = kvp.Value;
                    if (p.Global)
                    {
                        string label = kvp.Key;
                        fw.Write(label, true);
                        fw.Write((uint)(p.Offset + dataStart));
                    }
                }
                // Make pointers
                foreach (Pointer p in _pointers)
                {
                    _writer.Write((uint)(_labels[p.Label].Offset + dataStart), p.Offset);
                }
                ms.Position = 0;
                ms.CopyTo(fw.BaseStream);
            }
        }
    }

    private void ParseDir(AbsolutePath dir)
    {
        foreach (AbsolutePath file in dir.GlobFiles("*.txt"))
        {
            ParseFile(file);
        }
        foreach (string d in Directory.GetDirectories(dir))
        {
            ParseDir((AbsolutePath)d);
        }
    }
    private void ParseFile(string path)
    {
        string[] lines = File.ReadAllLines(path);
        var defines = new Dictionary<string, string>();
        bool readingLabel = false;
        bool globalLabel = false;
        bool readingDefine = false;
        string define1 = null;
        bool readingArg = false;
        bool readingCmd = false;
        bool readingText = false; // Text can span multiple lines and can include whitespace
        int curArg = -1;
        Type[] cmdArgTypes = null;
        string str = string.Empty;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (!readingText && string.IsNullOrWhiteSpace(line))
            {
                continue; // Skip empty lines if they're not text
            }
            for (int ic = 0; ic < line.Length; ic++)
            {
                char c = line[ic];
                if (!readingText && char.IsWhiteSpace(c))
                {
                    OnWhiteSpaceOrEndOfFile();
                    continue;
                }
                str += c;
                if (readingText)
                {
                    TryFindEndOfString();
                    continue;
                }
                if (str.EndsWith(CommentChars))
                {
                    str = str.Substring(0, str.Length - CommentChars.Length);
                    readingCmd = false;
                    readingDefine = false;
                    readingArg = false;
                    curArg = -1;
                    break; // Stop reading from here
                }
                if (readingCmd || readingDefine || readingArg || readingLabel || readingText)
                {
                    continue;
                }
                // Create a local label like "#Label"
                if (str.StartsWith(LocalLabelChars))
                {
                    readingLabel = true;
                    globalLabel = false;
                    str = string.Empty;
                    continue;
                }
                // Create a global label like "@Label"
                if (str.StartsWith(GlobalLabelChars))
                {
                    readingLabel = true;
                    globalLabel = true;
                    str = string.Empty;
                    continue;
                }
                if (str.StartsWith(TextChars))
                {
                    readingText = true;
                    str = string.Empty;
                    continue;
                }
                if (curArg == -1)
                {
                    readingCmd = true;
                }
                else
                {
                    readingArg = true;
                }
            }
            if (!readingText)
            {
                if (str != string.Empty)
                {
                    OnWhiteSpaceOrEndOfFile();
                }
                if (curArg != -1 && curArg < cmdArgTypes.Length)
                {
                    throw new Exception("Too few arguments");
                }
            }
        }
        if (readingText)
        {
            throw new Exception("Did not find end of string");
        }

        void TryFindEndOfString()
        {
            if (str.EndsWith(TextChars) && str.Length - 2 != '\\') // If we hit the end of the string and it's not a \" literal
            {
                str = str.Substring(0, str.Length - 1);
                WriteString(str);
                readingText = false;
                str = string.Empty;
            }
        }
        void OnWhiteSpaceOrEndOfFile()
        {
            if (readingCmd)
            {
                if (str.StartsWith(DefineChars))
                {
                    readingDefine = true;
                    curArg = 0;
                    readingCmd = false;
                    str = string.Empty;
                    return;
                }
                if (str.StartsWith(MovementPrefix))
                {
                    str = str.Substring(MovementPrefix.Length);
                    _writer.Write(Enum.Parse<ScriptMovement>(str));
                    readingCmd = false;
                    str = string.Empty;
                    return;
                }
                foreach (ScriptCommand cmd in ScriptBuilderHelper.Commands)
                {
                    if (str.Equals(cmd.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        _writer.Write(cmd);
                        cmdArgTypes = ScriptBuilderHelper.CommandArgs[cmd];
                        curArg = cmdArgTypes.Length == 0 ? -1 : 0;
                        readingCmd = false;
                        str = string.Empty;
                        return;
                    }
                }
                throw new Exception("Failed to parse command");
            }
            if (readingLabel)
            {
                _labels.Add(str, new Pair { Global = globalLabel, Offset = _writer.BaseStream.Position });
                readingLabel = false;
                str = string.Empty;
                return;
            }
            if (readingDefine)
            {
                if (curArg == 0)
                {
                    define1 = str;
                    curArg = 1;
                    str = string.Empty;
                }
                else
                {
                    defines.Add(define1, str);
                    readingDefine = false;
                    curArg = -1;
                    define1 = null;
                    str = string.Empty;
                }
                readingArg = false;
                return;
            }
            if (readingArg)
            {
                if (curArg >= cmdArgTypes.Length)
                {
                    throw new Exception("Too many arguments");
                }
                WriteArg(cmdArgTypes[curArg++], str, defines);
                if (curArg >= cmdArgTypes.Length)
                {
                    curArg = -1;
                }
                readingArg = false;
                str = string.Empty;
                return;
            }
        }
    }

    private static bool IsTypeVarAble(Type type)
    {
        if (type.IsEquivalentTo(typeof(Var)))
        {
            return false;
        }

        if (type.IsEnum)
        {
            type = Enum.GetUnderlyingType(type);
        }
        switch (type.FullName)
        {
            case "System.Byte":
            case "System.SByte":
            case "System.Int16":
            case "System.UInt16": return true;
        }
        return false;
    }
    private void WriteArg(Type argType, string str, Dictionary<string, string> defines)
    {
        switch (argType.FullName)
        {
            // Pointers
            case "System.Void*":
            {
                _pointers.Add(new Pointer { Label = str, Offset = _writer.BaseStream.Position });
                _writer.Write(0u); // Write a nullptr which we will update later
                break;
            }
            // Write an ID like "Map.TestMapC" or "0"
            case "System.String":
            {
                int index = str.IndexOf('.');
                if (index == -1)
                {
                    // Fallback to an int
                    _writer.Write((int)ParseInt(str));
                    break;
                }
                index++; // Include the '.'
                string prefix = str.Substring(0, index);
                if (!ScriptBuilderHelper.StringDefines.TryGetValue(prefix, out IdList idList))
                {
                    throw new ArgumentOutOfRangeException(nameof(str));
                }
                str = str.Substring(index);
                index = idList[str];
                if (index == -1)
                {
                    throw new Exception($"\"{str}\" was not a valid string for \"{prefix}\"");
                }
                _writer.Write(index);
                break;
            }
            // Write a float value like 400 or 400.0
            // Write a defined value
            case "System.Single":
            case "System.Double":
            {
                ParseAndWriteFloatValue(str, argType, defines);
                break;
            }
            // Write an enum like "Species.Bulbasaur" (can use var instead if the enum type is byte or short)
            // Write an int value like 0x400 or 400
            // Write a defined value
            default:
            {
                ParseAndWriteIntValue(str, argType, defines);
                break;
            }
        }
    }
    private void WriteString(string str)
    {
        int i = 0;
        while (i < str.Length)
        {
            char c = str[i];
            if (c == '\r')
            {
                i++;
                continue; // Ignore carriage return
            }
            bool foundReplacement = false;
            for (int m = 0; m < ScriptBuilderHelper.TextReplacements.Length; m++)
            {
                (string oldChars, string newChars) = ScriptBuilderHelper.TextReplacements[m];
                int ol = oldChars.Length;
                if (i + ol <= str.Length && str.Substring(i, ol) == oldChars)
                {
                    i += ol;
                    foundReplacement = true;
                    _writer.Write(newChars, false); // Write replacement without null terminator
                    break;
                }
            }
            if (!foundReplacement)
            {
                i++;
                _writer.Write(c); // Write single char
            }
        }
        _writer.Write('\0'); // Write null terminator
    }

    private static string GetDefineValue(string str, Dictionary<string, string> defines)
    {
        // Parse defines over and over until we get a final value for this string
        while (defines.TryGetValue(str, out string got))
        {
            str = got;
        }
        return str;
    }
    // Does not handle string type
    private void ParseAndWriteIntValue(string str, Type targetType, Dictionary<string, string> defines)
    {
        str = GetDefineValue(str, defines);
        // Check if we should try to parse an enum
        bool varAble = IsTypeVarAble(targetType);
        foreach (KeyValuePair<Type, string> tup in ScriptBuilderHelper.EnumDefines)
        {
            string prefix = tup.Value;
            if (!str.StartsWith(prefix))
            {
                continue;
            }
            // Enum type found, find out if we should write a link to a var or just its value
            str = str.Substring(prefix.Length);

            Type type = tup.Key;
            // Write a link to the var's value
            if (varAble && type.IsEquivalentTo(typeof(Var)))
            {
                uint wVal = ushort.MaxValue + 1 + Convert.ToUInt32(Enum.Parse<Var>(str));
                _writer.Write(wVal);
                return;
            }

            WriteIntValue(Enum.Parse(type, str), targetType);
            return;
        }

        // Did not find an enum value, so write just a parsed int value
        WriteIntValue(ParseInt(str), targetType);
    }
    private void WriteIntValue(object value, Type targetType)
    {
        // Var index should be written as Var's underlying type
        if (targetType.IsEquivalentTo(typeof(Var)))
        {
            _writer.Write((Var)Enum.ToObject(typeof(Var), value));
            return;
        }

        if (targetType.IsEnum)
        {
            targetType = targetType.GetEnumUnderlyingType();
        }
        switch (targetType.FullName)
        {
            case "System.Byte":
            case "System.SByte":
            case "System.Int16":
            case "System.UInt16":
            case "System.Int32":
            case "System.UInt32": _writer.Write(Convert.ToInt32(value)); break;
            case "System.Int64":
            case "System.UInt64": _writer.Write(Convert.ToInt64(value)); break;
            default: throw new Exception();
        }
    }
    private void ParseAndWriteFloatValue(string str, Type targetType, Dictionary<string, string> defines)
    {
        str = GetDefineValue(str, defines);
        WriteFloatValue(ParseFloat(str), targetType);
    }
    private void WriteFloatValue(object value, Type targetType)
    {
        switch (targetType.FullName)
        {
            case "System.Single": _writer.Write(Convert.ToSingle(value)); break;
            case "System.Double": _writer.Write(Convert.ToDouble(value)); break;
            default: throw new Exception();
        }
    }

    // TODO: Cannot have a define do math on another define; it just gets passed down here with the other define still in the str
    private static readonly CultureInfo _enUS = CultureInfo.GetCultureInfo("en-US");
    private long ParseInt(string value)
    {
        // First try regular values like "40" and "0x20"
        if (value.StartsWith("0x"))
        {
            if (long.TryParse(value.AsSpan(2), NumberStyles.HexNumber, _enUS, out long hex))
            {
                return hex;
            }
        }
        else if (long.TryParse(value, NumberStyles.Integer, _enUS, out long dec))
        {
            return dec;
        }

        // Then check if it's math
        bool foundMath = false;
        string str = string.Empty;
        long ret = 0;
        bool add = true, sub = false, mul = false, div = false; // Add first, so the initial value is set
        void DoOp()
        {
            if (add)
            {
                ret += ParseInt(str);
            }
            else if (sub)
            {
                ret -= ParseInt(str);
            }
            else if (mul)
            {
                ret *= ParseInt(str);
            }
            else if (div)
            {
                ret /= ParseInt(str);
            }
        }
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsWhiteSpace(c))
            {
                continue; // White space does nothing here
            }

            if (c == '+' || c == '-' || c == '*' || c == '/')
            {
                DoOp();
                add = c == '+'; sub = c == '-'; mul = c == '*'; div = c == '/';
                str = string.Empty;
                foundMath = true;
            }
            else
            {
                str += c;
            }
        }
        if (foundMath)
        {
            DoOp(); // Handle last
            return ret;
        }
        throw new FormatException(value);
    }
    private double ParseFloat(string value)
    {
        // First try regular values like "40" and "40.0"
        if (double.TryParse(value, NumberStyles.AllowDecimalPoint, _enUS, out double dec))
        {
            return dec;
        }

        // Then check if it's math
        bool foundMath = false;
        string str = string.Empty;
        double ret = 0d;
        bool add = true, sub = false, mul = false, div = false; // Add first, so the initial value is set
        void DoOp()
        {
            if (add)
            {
                ret += ParseFloat(str);
            }
            else if (sub)
            {
                ret -= ParseFloat(str);
            }
            else if (mul)
            {
                ret *= ParseFloat(str);
            }
            else if (div)
            {
                ret /= ParseFloat(str);
            }
        }
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsWhiteSpace(c))
            {
                continue; // White space does nothing here
            }

            if (c == '+' || c == '-' || c == '*' || c == '/')
            {
                DoOp();
                add = c == '+'; sub = c == '-'; mul = c == '*'; div = c == '/';
                str = string.Empty;
                foundMath = true;
            }
            else
            {
                str += c;
            }
        }
        if (foundMath)
        {
            DoOp(); // Handle last
            return ret;
        }
        throw new FormatException(value);
    }
}

