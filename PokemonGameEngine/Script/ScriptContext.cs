using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Script
{
    internal sealed partial class ScriptContext : IDisposable
    {
        private readonly EndianBinaryReader _reader;
        private readonly Stack<long> _callStack = new Stack<long>();
        public bool TempDone;

        public ScriptContext(EndianBinaryReader r)
        {
            _reader = r;
        }

        public void Dispose()
        {
            TempDone = true;
            _reader.Dispose();
        }
    }
}
