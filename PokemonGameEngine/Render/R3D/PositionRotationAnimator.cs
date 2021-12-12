namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal interface IPositionRotationAnimator
    {
        bool IsDone { get; }

        bool Update(ref PositionRotation result);
    }

    internal sealed class PositionRotationAnimator : IPositionRotationAnimator
    {
        private readonly PositionRotation _from;
        private readonly PositionRotation _to;
        private readonly float _duration;
        private float _time;

        public bool IsDone { get; private set; }

        public PositionRotationAnimator(in PositionRotation from, in PositionRotation to, float seconds)
        {
            _from = from;
            _to = to;
            _duration = seconds;
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
            result.Slerp(_from, _to, progress);
            return IsDone;
        }
    }

    internal sealed class PositionRotationAnimatorSplit : IPositionRotationAnimator
    {
        private readonly PositionRotation _from;
        private readonly PositionRotation _to;
        private readonly float _posDuration;
        private readonly float _rotDuration;
        private float _posTime;
        private float _rotTime;

        public bool IsPosDone;
        public bool IsRotDone;
        public bool IsDone => IsPosDone && IsRotDone;

        public PositionRotationAnimatorSplit(in PositionRotation from, in PositionRotation to, float posSeconds, float rotSeconds)
        {
            _from = from;
            _to = to;
            _posDuration = posSeconds;
            _rotDuration = rotSeconds;
        }

        public bool Update(ref PositionRotation result)
        {
            if (IsDone)
            {
                return true;
            }

            if (!IsPosDone)
            {
                _posTime += Display.DeltaTime;
                float progress = _posTime / _posDuration;
                if (progress >= 1f)
                {
                    progress = 1f;
                    IsPosDone = true;
                }
                result.LerpPosition(_from.Position, _to.Position, progress);
            }
            if (!IsRotDone)
            {
                _rotTime += Display.DeltaTime;
                float progress = _rotTime / _rotDuration;
                if (progress >= 1f)
                {
                    progress = 1f;
                    IsRotDone = true;
                }
                result.SlerpRotation(_from.Rotation, _to.Rotation, progress);
            }
            return IsDone;
        }
    }
}
