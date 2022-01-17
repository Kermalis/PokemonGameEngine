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

        public static float Smooth2(float v)
        {
            if (v < 0.5f)
            {
                return v * v * 2f;
            }

            float x = (-2f * v) + 2;
            return 1f - (x * x * 0.5f);
        }
        public static float Smooth3(float v)
        {
            if (v < 0.5f)
            {
                return v * v * v * 2f * 2f;
            }

            float x = (-2f * v) + 2;
            return 1f - (x * x * x * 0.5f);
        }

        // Bell curves are the following function where (a > 1):
        // f(v) = (4^a) * ((v * (1 - v))^a)
        public static float BellCurve2(float v)
        {
            v *= 1f - v;
            return v * v * 4f * 4f;
        }
        public static float BellCurve3(float v)
        {
            v *= 1f - v;
            return v * v * v * 4f * 4f * 4f;
        }
    }
}
