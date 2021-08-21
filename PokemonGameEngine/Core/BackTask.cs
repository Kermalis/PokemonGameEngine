namespace Kermalis.PokemonGameEngine.Core
{
    internal delegate void BackTaskAction(BackTask task);

    internal sealed class BackTask
    {
        public BackTask Next;
        public BackTask Prev;

        public BackTaskAction Action;
        public object Data;
        public object Tag;

        public readonly int Priority; // Higher priorities go first

        public BackTask(BackTaskAction action, int priority, object data = null, object tag = null)
        {
            Action = action;
            Priority = priority;
            Data = data;
            Tag = tag;
        }

        public void Dispose()
        {
            // Do not dispose next or prev so we can continue looping after this gets removed
            Action = null;
            Data = null;
            Tag = null;
        }
    }

    internal sealed class TaskList
    {
        private BackTask _first;
        public int Count { get; private set; }

        public void Add(BackTask task)
        {
            if (_first is null)
            {
                _first = task;
                Count = 1;
                return;
            }
            BackTask t = _first;
            while (true)
            {
                if (t.Priority < task.Priority)
                {
                    // The new task has a higher priority than t, so insert new before t
                    if (t == _first)
                    {
                        _first = t;
                    }
                    else
                    {
                        BackTask prev = t.Prev;
                        task.Prev = prev;
                        prev.Next = task;
                    }
                    t.Prev = task;
                    task.Next = t;
                    Count++;
                    return;
                }
                // Iterate to next task if there is one
                BackTask next = t.Next;
                if (next is null)
                {
                    // The new task is the lowest priority or tied for it, so place new at the last position
                    t.Next = task;
                    task.Prev = t;
                    Count++;
                    return;
                }
                t = next;
            }
        }
        public void Add(BackTaskAction action, int priority, object data = null, object tag = null)
        {
            Add(new BackTask(action, priority, data: data, tag: tag));
        }
        public void RemoveAndDispose(BackTask task)
        {
            if (task == _first)
            {
                BackTask next = task.Next;
                if (next is not null)
                {
                    next.Prev = null;
                }
                _first = next;
            }
            else
            {
                BackTask prev = task.Prev;
                BackTask next = task.Next;
                if (next is not null)
                {
                    next.Prev = prev;
                }
                prev.Next = next;
            }
            task.Dispose();
            Count--;
        }

        public void RemoveAll()
        {
            for (BackTask t = _first; t is not null; t = t.Next)
            {
                t.Dispose();
            }
            _first = null;
            Count = 0;
        }
        public void RemoveAllWithTag(object tag)
        {
            for (BackTask t = _first; t is not null; t = t.Next)
            {
                if (Equals(t.Tag, tag))
                {
                    RemoveAndDispose(t);
                }
            }
        }
        public void RemoveAllWithoutTag(object tag)
        {
            for (BackTask t = _first; t is not null; t = t.Next)
            {
                if (!Equals(t.Tag, tag))
                {
                    RemoveAndDispose(t);
                }
            }
        }

        public void RunTasks()
        {
            for (BackTask t = _first; t is not null; t = t.Next)
            {
                t.Action(t);
            }
        }
        public void RunTasksWithTag(object tag)
        {
            for (BackTask t = _first; t is not null; t = t.Next)
            {
                if (Equals(t.Tag, tag))
                {
                    t.Action(t);
                }
            }
        }
        public void RunTasksWithoutTag(object tag)
        {
            for (BackTask t = _first; t is not null; t = t.Next)
            {
                if (!Equals(t.Tag, tag))
                {
                    t.Action(t);
                }
            }
        }

        public bool TryGetTask(BackTaskAction action, out BackTask task)
        {
            for (BackTask t = _first; t is not null; t = t.Next)
            {
                if (Equals(t.Action, action))
                {
                    task = t;
                    return true;
                }
            }
            task = default;
            return false;
        }
        public bool TryGetTaskWithTag(object tag, out BackTask task)
        {
            for (BackTask t = _first; t is not null; t = t.Next)
            {
                if (Equals(t.Tag, tag))
                {
                    task = t;
                    return true;
                }
            }
            task = default;
            return false;
        }
    }
}
