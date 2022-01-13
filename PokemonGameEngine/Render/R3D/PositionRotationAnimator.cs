using System;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal sealed class PositionRotationAnimator
    {
        public enum Method : byte
        {
            Linear,
            Smooth
        }

        private readonly Method _method;
        private readonly PositionRotation _from;
        private readonly PositionRotation _to;
        private readonly float _duration;
        private float _time;

        public bool IsDone { get; private set; }

        public PositionRotationAnimator(Method m, in PositionRotation from, in PositionRotation to, float seconds)
        {
            _method = m;
            _from = from;
            _to = to;
            _duration = seconds;
        }

        private float ApplyMethod(float input)
        {
            switch (_method)
            {
                case Method.Linear: return input;
                case Method.Smooth: return Easing.Smooth3(input);
            }
            throw new Exception();
        }

        public bool Update(ref PositionRotation result)
        {
            if (IsDone)
            {
                return true;
            }

            _time += Display.DeltaTime;
            float progress = _time / _duration;
            if (progress >= 1f)
            {
                progress = 1f;
                IsDone = true;
            }
            else
            {
                progress = ApplyMethod(progress);
            }
            result.Slerp(_from, _to, progress);
            return IsDone;
        }
    }
}
