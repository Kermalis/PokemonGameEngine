using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    public sealed class IdList : IEnumerable<string>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private readonly string _path;
        private readonly List<string> _entries = new();

        internal IdList(string path)
        {
            if (File.Exists(path))
            {
                using (StreamReader s = File.OpenText(path))
                {
                    string key;
                    while ((key = s.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            Add(key);
                        }
                    }
                }
            }
            else
            {
                File.CreateText(path).Dispose();
            }
            _path = path;
        }

        internal int this[string key]
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
        internal string this[int id]
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

        internal int Add(string key)
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
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, key, count));
            return count;
        }

        internal void Save()
        {
            using (var s = new StreamWriter(File.Create(_path)))
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    s.WriteLine(_entries[i]);
                }
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            int count = _entries.Count;
            for (int i = 0; i < count; i++)
            {
                yield return _entries[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
