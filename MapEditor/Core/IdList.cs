using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    internal sealed class IdList
    {
        private readonly string _path;
        private readonly List<string> _entries = new List<string>();

        public IdList(string path)
        {
            _path = path;
            using (StreamReader s = File.OpenText(_path))
            {
                string key;
                while ((key = s.ReadLine()) != null)
                {
                    Add(key);
                }
            }
        }

        public int this[string key]
        {
            get
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (key == _entries[i])
                    {
                        return i;
                    }
                }
                return -1;
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

        public int Add(string key)
        {
            int count = _entries.Count;
            for (int i = 0; i < count; i++)
            {
                if (key == _entries[i])
                {
                    throw new ArgumentOutOfRangeException(nameof(key));
                }
            }
            _entries.Add(key);
            return count;
        }

        public void Save()
        {
            using (var s = new StreamWriter(File.OpenWrite(_path)))
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    s.WriteLine(_entries[i]);
                }
            }
        }
    }
}
