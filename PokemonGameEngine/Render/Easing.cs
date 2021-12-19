namespace Kermalis.PokemonGameEngine.Render
{
    /// <summary>Provides animation helper functions. Input and output values are always between 0 and 1 inclusive.</summary>
    internal static class Easing
    {
        public static float SmoothStart2(float v)
        {
            return v * v;
        }
        public static float SmoothStart3(float v)
        {
            return v * v * v;
        }

        public static float SmoothStop2(float v)
        {
            v = 1f - v;
            return 1f - (v * v);
        }
        public static float SmoothStop3(float v)
        {
            v = 1f - v;
            return 1f - (v * v * v);
        }

        // Bell curves are the following function where (a > 1):
        // f(v) = (4^a) * ((v * (1 - v))^a)
        public static float BellCurve2(float v)
        {
            const float mod = 4f * 4f;
            v *= 1f - v;
            return v * v * mod;
        }
        public static float BellCurve3(float v)
        {
            const float mod = 4f * 4f * 4f;
            v *= 1f - v;
            return v * v * v * mod;
        }
    }
}
