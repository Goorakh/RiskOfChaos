using System;
using UnityEngine;

namespace RiskOfChaos.Utilities.Interpolation
{
    public class InterpolationState
    {
        public ValueInterpolationFunctionType InterpolationType { get; private set; }

        float _interpolationStartTime = -1f;
        float _interpolationDuration;

        bool _invert;

        float interpolationTimeElapsed => _interpolationStartTime >= 0f ? (Time.unscaledTime - _interpolationStartTime) : -1f;

        public bool IsInterpolating
        {
            get
            {
                float timeElapsed = interpolationTimeElapsed;
                return timeElapsed >= 0f && timeElapsed <= _interpolationDuration;
            }
        }

        public float CurrentFraction
        {
            get
            {
                if (!IsInterpolating)
                    throw new InvalidOperationException("Not interpolating");

                float start, end;
                if (!_invert)
                {
                    start = 0f;
                    end = 1f;
                }
                else
                {
                    start = 1f;
                    end = 0f;
                }

                return InterpolationType.Interpolate(start, end, Mathf.Clamp01(interpolationTimeElapsed / _interpolationDuration));
            }
        }

        public delegate void OnTickDelegate(float fraction);

        public event OnTickDelegate OnTick;
        public event Action OnFinish;

        public void StartInterpolating(ValueInterpolationFunctionType type, float duration, bool invert)
        {
            _interpolationStartTime = Time.unscaledTime;

            InterpolationType = type;
            _interpolationDuration = duration;
            _invert = invert;
        }

        public void Update()
        {
            OnTick?.Invoke(CurrentFraction);
        }

        public void OnInterpolationFinished()
        {
            OnFinish?.Invoke();
        }
    }
}
