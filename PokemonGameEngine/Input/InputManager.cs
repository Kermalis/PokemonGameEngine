namespace Kermalis.PokemonGameEngine.Input
{
    internal static class InputManager
    {
        public static void Init()
        {
            Controller.TryAttachController();
        }

        public static void Prepare()
        {
            Keyboard.Prepare();
            Controller.Prepare();
        }

        public static bool IsDown(Key k)
        {
            return Keyboard.IsDown(k)
                || Controller.IsDown(k);
        }
        public static bool JustPressed(Key k)
        {
            return Keyboard.JustPressed(k)
                || Controller.JustPressed(k);
        }
        public static bool JustReleased(Key k)
        {
            return Keyboard.JustReleased(k)
                || Controller.JustReleased(k);
        }

        public static void Quit()
        {
            Controller.Quit();
        }
    }
}
