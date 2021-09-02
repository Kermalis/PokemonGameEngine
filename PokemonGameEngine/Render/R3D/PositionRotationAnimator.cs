using System;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal interface IPositionRotationAnimator
    {
        bool IsDone { get; }

        bool Update(ref PositionRotation result);
    }

    internal sealed class PositionRotationAnimator : IPositionRotationAnimator
    {
        private TimeSpan _movementCur;
        private readonly TimeSpan _movementEnd;
        private readonly PositionRotation _from;
        private readonly PositionRotation _to;

        public bool IsDone { get; private set; }

        public PositionRotationAnimator(in PositionRotation from, in PositionRotation to, double milliseconds)
        {
            _from = from;
            _to = to;
            _movementCur = new TimeSpan();
            _movementEnd = TimeSpan.FromMilliseconds(milliseconds);
        }

        public bool Update(ref PositionRotation result)
        {
            if (IsDone)
            {
                return true;
            }
            float progress = (float)Renderer.GetAnimationProgress(_movementEnd, ref _movementCur);
            result.Slerp(_from, _to, progress);
            IsDone = progress >= 1;
            return IsDone;
        }
    }

    internal sealed class PositionRotationAnimatorSplit : IPositionRotationAnimator
    {
        private TimeSpan _positionCur;
        private TimeSpan _movementCur;
        private readonly TimeSpan _positionEnd;
        private readonly TimeSpan _movementEnd;
        private readonly PositionRotation _from;
        private readonly PositionRotation _to;

        public bool IsPosDone;
        public bool IsRotDone;
        public bool IsDone => IsPosDone && IsRotDone;

        public PositionRotationAnimatorSplit(in PositionRotation from, in PositionRotation to, double posMilliseconds, double rotMilliseconds)
        {
            _from = from;
            _to = to;
            _positionCur = new TimeSpan();
            _movementCur = new TimeSpan();
            _positionEnd = TimeSpan.FromMilliseconds(posMilliseconds);
            _movementEnd = TimeSpan.FromMilliseconds(rotMilliseconds);
        }

        public bool Update(ref PositionRotation result)
        {
            if (IsDone)
            {
                return true;
            }
            if (!IsPosDone)
            {
                float progress = (float)Renderer.GetAnimationProgress(_positionEnd, ref _positionCur);
                result.LerpPosition(_from.Position, _to.Position, progress);
                IsPosDone = progress >= 1;
            }
            if (!IsRotDone)
            {
                float progress = (float)Renderer.GetAnimationProgress(_movementEnd, ref _movementCur);
                result.SlerpRotation(_from.Rotation, _to.Rotation, progress);
                IsRotDone = progress >= 1;
            }
            return IsDone;
        }
    }
}
