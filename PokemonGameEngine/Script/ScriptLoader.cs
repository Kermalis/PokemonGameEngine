using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Util;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Script
{
    internal static class ScriptLoader
    {
        private static readonly Dictionary<string, uint> _globalScriptOffsets;

        private const string _scriptExtension = ".bin";
        private const string _scriptPath = "Script.";
        private const string _scriptFile = _scriptPath + "Scripts" + _scriptExtension;
        static ScriptLoader()
        {
            using (EndianBinaryReader r = GetReader())
            {
                int count = r.ReadInt32();
                _globalScriptOffsets = new Dictionary<string, uint>(count);
                for (int i = 0; i < count; i++)
                {
                    _globalScriptOffsets.Add(r.ReadStringNullTerminated(), r.ReadUInt32());
                }
            }
        }

        private static EndianBinaryReader GetReader()
        {
            return new EndianBinaryReader(Utils.GetResourceStream(_scriptFile), encoding: EncodingType.UTF16);
        }

        public static void LoadScript(string label)
        {
            if (!_globalScriptOffsets.TryGetValue(label, out uint offset))
            {
                throw new Exception($"Could not find script with a global label of \"{label}\"");
            }
            EndianBinaryReader r = GetReader();
            r.BaseStream.Position = offset;
            Game.Game.Scripts.Add(new ScriptContext(r));
        }
    }
}
