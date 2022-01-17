namespace Kermalis.PokemonGameEngine.Core
{
    internal delegate void BackTaskAction(BackTask task);

    internal sealed class BackTask : IConnectedListObject<BackTask>
    {
        public BackTask Next { get; set; }
        public BackTask Prev { get; set; }

        public BackTaskAction Action;
        public object Data;
        public object Tag;

        /// <summary>Higher priorities go first</summary>
        public readonly int Priority;

        public BackTask(BackTaskAction action, int priority, object data = null, object tag = null)
        {
            Action = action;
            Priority = priority;
            Data = data;
            Tag = tag;
        }

        public static int Sorter(BackTask t1, BackTask t2)
        {
            if (t1.Priority > t2.Priority)
            {
                return -1;
            }
            if (t1.Priority == t2.Priority)
            {
                return 0;
            }
            return 1;
        }

        public void Dispose()
        {
            // Do not dispose next or prev so we can continue looping after this gets removed
            Action = null;
            Data = null;
            Tag = null;
        }
    }
}
