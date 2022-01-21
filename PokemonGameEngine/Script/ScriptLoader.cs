using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Script
{
    internal static class ScriptLoader
    {
        private const string FILE = @"Script\Scripts.bin";

        private static readonly Dictionary<string, uint> _globalScriptOffsets;

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
            return new EndianBinaryReader(File.OpenRead(AssetLoader.GetPath(FILE)), encoding: EncodingType.UTF16);
        }

        public static ScriptContext LoadScript(string label, Vec2I viewSize)
        {
            if (!_globalScriptOffsets.TryGetValue(label, out uint offset))
            {
                throw new Exception($"Could not find script with a global label of \"{label}\"");
            }
            EndianBinaryReader r = GetReader();
            r.BaseStream.Position = offset;
            return new ScriptContext(viewSize, r);
        }
    }
}
