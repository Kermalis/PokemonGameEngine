using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
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

    private static readonly AbsolutePath ScriptPath = RootDirectory / "PokemonGameEngine" / "Assets" / "Script";
    private static readonly AbsolutePath ScriptOutputPath = ScriptPath / "Scripts.bin";
    private const string CommentChars = "//";
    private const string LocalLabelChars = "#";
    private const string GlobalLabelChars = "@";

    private static readonly Dictionary<Type, string> _enumDefines = new Dictionary<Type, string>()
    {
        { typeof(PBEForm), "Form." },
        { typeof(PBEItem), "Item." },
        { typeof(PBESpecies), "Species." }
    };

    private readonly Dictionary<string, Pair> _labels = new Dictionary<string, Pair>();
    private readonly List<Pointer> _pointers = new List<Pointer>();
    private EndianBinaryWriter _writer;

    private void BuildScripts()
    {
        using (var ms = new MemoryStream())
        using (_writer = new EndianBinaryWriter(ms, encoding: EncodingType.UTF16))
        {
            foreach (string file in Directory.EnumerateFiles(ScriptPath, "*.txt"))
            {
                ParseFile(file);
            }
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

    // Write an enum like "Species.Bulbasaur"
    private void WriteEnum(Type argType, string prefix, string str)
    {
        if (str.StartsWith(prefix))
        {
            str = str.Substring(prefix.Length);
            _writer.Write((Enum)Enum.Parse(argType, str));
            return;
        }
        throw new Exception($"Failed to parse enum of type \"{argType}\"");
    }
    private void WriteArg(Type argType, string str)
    {
        switch (argType.FullName)
        {
            case "System.Byte": _writer.Write((byte)ParseInt(str)); break;
            case "System.SByte": _writer.Write((sbyte)ParseInt(str)); break;
            case "System.Int16": _writer.Write((short)ParseInt(str)); break;
            case "System.UInt16": _writer.Write((ushort)ParseInt(str)); break;
            case "System.Int32": _writer.Write((int)ParseInt(str)); break;
            case "System.UInt32": _writer.Write((uint)ParseInt(str)); break;
            case "System.Int64": _writer.Write(ParseInt(str)); break;
            case "System.UInt64": _writer.Write((ulong)ParseInt(str)); break;
            case "System.Void*":
            {
                _pointers.Add(new Pointer { Label = str, Offset = _writer.BaseStream.Position });
                _writer.Write(0u); // Write a nullptr which we will update later
                break;
            }
            default:
            {
                if (!_enumDefines.TryGetValue(argType, out string prefix))
                {
                    throw new ArgumentOutOfRangeException(nameof(argType));
                }
                WriteEnum(argType, prefix, str);
                break;
            }
        }
    }

    private void ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return; // Skip empty lines
        }

        bool readingLabel = false;
        bool globalLabel = false;
        bool readingArg = false;
        bool readingCmd = false;
        int curArg = -1;
        Type[] cmdArgTypes = null;
        string str = string.Empty;
        void WriteThing()
        {
            if (readingCmd)
            {
                foreach (ScriptCommand cmd in ScriptBuilderHelper.Commands)
                {
                    if (str == cmd.ToString())
                    {
                        _writer.Write(cmd);
                        cmdArgTypes = ScriptBuilderHelper.CommandArgs[cmd];
                        curArg = 0;
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
            }
            else if (readingArg)
            {
                if (curArg >= cmdArgTypes.Length)
                {
                    throw new Exception("Too many arguments");
                }
                WriteArg(cmdArgTypes[curArg++], str);
                readingArg = false;
                str = string.Empty;
            }
        }
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (char.IsWhiteSpace(c))
            {
                WriteThing();
                continue;
            }
            str += c;
            if (str.EndsWith(CommentChars))
            {
                str = str.Substring(0, str.Length - CommentChars.Length);
                break; // Stop reading from here
            }
            if (readingCmd || readingArg || readingLabel)
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
            if (curArg == -1)
            {
                readingCmd = true;
            }
            else
            {
                readingArg = true;
            }
        }
        if (str != string.Empty)
        {
            WriteThing();
        }
        if (curArg != -1 && curArg < cmdArgTypes.Length)
        {
            throw new Exception("Too few arguments");
        }
    }
    private void ParseFile(string path)
    {
        string[] lines = File.ReadAllLines(path);
        for (int i = 0; i < lines.Length; i++)
        {
            ParseLine(lines[i]);
        }
    }

    private static readonly CultureInfo _enUS = CultureInfo.GetCultureInfo("en-US");
    private long ParseInt(string value)
    {
        // First try regular values like "40" and "0x20"
        if (value.StartsWith("0x"))
        {
            if (long.TryParse(value.Substring(2), NumberStyles.HexNumber, _enUS, out long hex))
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
        throw new ArgumentOutOfRangeException(nameof(value));
    }
}

