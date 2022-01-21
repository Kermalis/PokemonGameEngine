using System;

namespace Kermalis.PokemonGameEngine.Core
{
    internal interface IConnectedListObject<T> : IDisposable
        where T : class
    {
        T Prev { get; set; }
        T Next { get; set; }
    }

    internal sealed class ConnectedList<T>
        where T : class, IConnectedListObject<T>
    {
        private readonly Func<T, T, int> _sorter;

        public T First { get; private set; }
        public int Count { get; private set; }

        public ConnectedList()
        {
            _sorter = NoSort;
        }
        public ConnectedList(Func<T, T, int> sorter)
        {
            _sorter = sorter;
        }

        public void Add(T newObj)
        {
            if (First is null)
            {
                First = newObj;
                Count = 1;
                return;
            }
            T o = First;
            while (true)
            {
                if (_sorter(newObj, o) < 0)
                {
                    // newObj goes before o, so insert newObj before o
                    if (o == First)
                    {
                        First = newObj; // newObj is now the first object
                    }
                    else
                    {
                        T prev = o.Prev; // Connect the one before and one after with newObj
                        newObj.Prev = prev;
                        prev.Next = newObj;
                    }
                    o.Prev = newObj;
                    newObj.Next = o;
                    Count++;
                    return;
                }
                // Iterate to next object if there is one
                T next = o.Next;
                if (next is null)
                {
                    // newObj is sorted last or tied for it, so place newObj at the last position
                    o.Next = newObj;
                    newObj.Prev = o;
                    Count++;
                    return;
                }
                o = next;
            }
        }
        public void Remove(T obj, bool dispose = true)
        {
            if (obj == First)
            {
                T next = obj.Next;
                if (next is not null) // This was not the only object
                {
                    next.Prev = null; // Make the next one the first one
                }
                First = next;
            }
            else // Not the first one so we have a previous one
            {
                T prev = obj.Prev;
                T next = obj.Next;
                if (next is not null) // This was not last
                {
                    next.Prev = prev; // Connect the previous and next together
                }
                prev.Next = next;
            }
            if (dispose)
            {
                obj.Dispose();
            }
            Count--;
        }

        public void Sort()
        {
            if (First is null)
            {
                return;
            }
            for (T o = First.Next; o is not null; o = o.Next)
            {
                T cur = o;
                T prev = o.Prev;
                while (_sorter(o, prev) < 0)
                {
                    T prev2 = prev.Prev;
                    T next = cur.Next;
                    prev.Next = next;
                    prev.Prev = cur;
                    cur.Next = prev;
                    cur.Prev = prev2;
                    if (next is not null)
                    {
                        next.Prev = prev;
                    }
                    if (prev2 is null)
                    {
                        First = cur;
                        break;
                    }
                    prev2.Next = cur;
                    prev = prev2;
                }
            }
        }

        public bool TryGet(Func<T, bool> selector, out T obj)
        {
            for (T o = First; o is not null; o = o.Next)
            {
                if (selector(o))
                {
                    obj = o;
                    return true;
                }
            }
            obj = default;
            return false;
        }

        private static int NoSort<TObj>(TObj a, TObj b)
        {
            return 0;
        }
    }
}
