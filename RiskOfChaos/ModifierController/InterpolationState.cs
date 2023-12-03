using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.ModifierController
{
    public struct InterpolationState
    {
        ValueInterpolationFunctionType _interpolationType;
        public readonly ValueInterpolationFunctionType InterpolationType => _interpolationType;

        Run.TimeStamp _interpolationStartTime = Run.TimeStamp.negativeInfinity;
        float _interpolationDuration;

        bool _invert;

        public readonly bool IsInterpolating => _interpolationStartTime.timeSince <= _interpolationDuration;

        public readonly float CurrentFraction
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

                return _interpolationType.Interpolate(start, end, Mathf.Clamp01(_interpolationStartTime.timeSince / _interpolationDuration));
            }
        }

        public InterpolationState()
        {
        }

        public void StartInterpolating(ValueInterpolationFunctionType type, float duration, bool invert)
        {
            _interpolationStartTime = Run.TimeStamp.now;

            _interpolationType = type;
            _interpolationDuration = duration;
            _invert = invert;
        }
    }
}
