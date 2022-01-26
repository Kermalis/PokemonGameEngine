using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Input
{
    internal sealed class PressData
    {
        public bool IsPressed;

        /// <summary><see langword="true"/> if this button was not pressed the previous frame but now is</summary>
        public bool IsNew;
        /// <summary><see langword="true"/> if the button was released this frame</summary>
        public bool WasReleased;

        public void Prepare()
        {
            IsNew = false;
            WasReleased = false;
        }
        public void OnChanged(bool down)
        {
            IsPressed = down;
            if (down)
            {
                IsNew = true;
            }
            else
            {
                WasReleased = true;
            }
        }

        public static Dictionary<T, PressData> CreateDict<T>(T[] arr)
        {
            var dict = new Dictionary<T, PressData>(arr.Length);
            for (int i = 0; i < arr.Length; i++)
            {
                dict.Add(arr[i], new PressData());
            }
            return dict;
        }
        public static void PrepareMany(IEnumerable<PressData> data)
        {
            foreach (PressData p in data)
            {
                p.Prepare();
            }
        }
    }
}
