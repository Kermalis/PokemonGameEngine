using Kermalis.PokemonGameEngine.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Overworld
{
    internal sealed class IdList
    {
        private readonly List<string> _entries = new List<string>();

        public IdList(string resource)
        {
            using (var s = new StreamReader(Utils.GetResourceStream(resource)))
            {
                string key;
                while ((key = s.ReadLine()) != null)
                {
                    for (int i = 0; i < _entries.Count; i++)
                    {
                        if (key == _entries[i])
                        {
                            throw new ArgumentOutOfRangeException(nameof(key));
                        }
                    }
                    _entries.Add(key);
                }
            }
        }

        public string this[int id]
        {
            get
            {
                if (id < 0 || id >= _entries.Count)
                {
                    return null;
                }
                return _entries[id];
            }
        }
    }
}
