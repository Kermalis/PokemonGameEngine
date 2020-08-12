using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

internal sealed class IdList : IEnumerable<string>
{
    private readonly List<string> _entries = new List<string>();

    public IdList(string path)
    {
        using (StreamReader s = File.OpenText(path))
        {
            string key;
            while ((key = s.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(key))
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
    }

    public int Count => _entries.Count;
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
