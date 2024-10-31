using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public sealed class GenericInterpolationComponent : MonoBehaviour, IInterpolationProvider
    {
        public bool DestroyOnCompleteOut = true;

        public InterpolationParameters InterpolationIn { get; set; } = InterpolationParameters.None;

        public InterpolationParameters InterpolationOut { get; set; } = InterpolationParameters.None;

        public Run.FixedTimeStamp InterpolationInStartTime = Run.FixedTimeStamp.positiveInfinity;

        public Run.FixedTimeStamp InterpolationOutStartTime = Run.FixedTimeStamp.positiveInfinity;

        public bool IsInterpolating { get; private set; }

        public float CurrentInterpolationFraction { get; private set; }

        public event Action OnInterpolationChanged;

        public event Action OnInterpolationOutComplete;

        void Start()
        {
            if (InterpolationInStartTime.isInfinity)
            {
                InterpolationInStartTime = Run.FixedTimeStamp.now;
            }

            updateInterpolation();
        }

        void Update()
        {
            updateInterpolation();
        }

        void updateInterpolation()
        {
            bool wasInterpolating = IsInterpolating;

            float interpolatingInFraction = 1f;
            bool isInterpolatingIn = false;
            if (!InterpolationInStartTime.isInfinity)
            {
                if (InterpolationIn.Duration > 0f)
                {
                    isInterpolatingIn = InterpolationInStartTime.timeSinceClamped <= InterpolationIn.Duration;

                    if (isInterpolatingIn)
                    {
                        interpolatingInFraction = Mathf.Clamp01(InterpolationInStartTime.timeSinceClamped / InterpolationIn.Duration);
                    }
                }
            }

            float interpolatingOutFraction = 1f;
            bool isInterpolatingOut = false;
            bool isInterpolationOutComplete = false;
            if (!InterpolationOutStartTime.isInfinity)
            {
                if (InterpolationOut.Duration > 0f)
                {
                    isInterpolatingOut = InterpolationOutStartTime.hasPassed;

                    if (isInterpolatingOut)
                    {
                        interpolatingOutFraction = 1f - Mathf.Clamp01(InterpolationOutStartTime.timeSinceClamped / InterpolationOut.Duration);
                    }
                }

                isInterpolationOutComplete = InterpolationOutStartTime.timeSince > Mathf.Max(0f, InterpolationOut.Duration);
            }

            IsInterpolating = isInterpolatingIn || isInterpolatingOut;

            float interpolatingFraction = interpolatingInFraction * interpolatingOutFraction;

            const float MAX_FRACTION_MOVE_DELTA = 3f;
            if (IsInterpolating)
            {
                CurrentInterpolationFraction = Mathf.MoveTowards(CurrentInterpolationFraction, interpolatingFraction, Time.unscaledDeltaTime * MAX_FRACTION_MOVE_DELTA);
            }
            else
            {
                CurrentInterpolationFraction = interpolatingFraction;
            }

            if (IsInterpolating || wasInterpolating != IsInterpolating)
            {
                OnInterpolationChanged?.Invoke();
            }

            if (wasInterpolating && isInterpolationOutComplete)
            {
#if DEBUG
                Log.Debug($"Finished interpolation out for {name}");
#endif

                OnInterpolationOutComplete?.Invoke();
            }
        }

        public void SetInterpolationParameters(InterpolationParameters parameters)
        {
            InterpolationIn = parameters;
            InterpolationOut = parameters;
        }

        public void InterpolateOutOrDestroy()
        {
            if (InterpolationOut.Duration > 0f)
            {
                OnInterpolationOutComplete += () =>
                {
                    Destroy(gameObject);
                };

                if (InterpolationOutStartTime.isInfinity)
                {
                    InterpolationOutStartTime = Run.FixedTimeStamp.now;
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
