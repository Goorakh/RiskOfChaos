using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    [DefaultExecutionOrder(-1)]
    public sealed class NetworkedInterpolationComponent : NetworkBehaviour, IInterpolationProvider
    {
        [SyncVar]
        RunTimeStamp _interpolationInStartTimeWrapper = Run.FixedTimeStamp.positiveInfinity;

        [SyncVar]
        public InterpolationParameters InterpolationIn = InterpolationParameters.None;

        InterpolationParameters IInterpolationProvider.InterpolationIn
        {
            get => InterpolationIn;
            set => InterpolationIn = value;
        }

        [SyncVar]
        RunTimeStamp _interpolationOutStartTimeWrapper = Run.FixedTimeStamp.positiveInfinity;

        [SyncVar]
        public InterpolationParameters InterpolationOut = InterpolationParameters.None;

        InterpolationParameters IInterpolationProvider.InterpolationOut
        {
            get => InterpolationOut;
            set => InterpolationOut = value;
        }

        public RunTimeStamp InterpolationInStartTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _interpolationInStartTimeWrapper;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _interpolationInStartTimeWrapper = value.ConvertTo(RunTimerType.Realtime);
        }

        public RunTimeStamp InterpolationOutStartTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _interpolationOutStartTimeWrapper;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _interpolationOutStartTimeWrapper = value.ConvertTo(RunTimerType.Realtime);
        }

        public bool IsInterpolating { get; private set; }

        public float CurrentInterpolationFraction { get; private set; }

        public event Action OnInterpolationChanged;

        public event Action OnInterpolationOutComplete;

        void Start()
        {
            updateInterpolation();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (InterpolationInStartTime.IsInfinity)
            {
                InterpolationInStartTime = Run.FixedTimeStamp.now;
            }
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
            if (!InterpolationInStartTime.IsInfinity)
            {
                if (InterpolationIn.Duration > 0f)
                {
                    isInterpolatingIn = InterpolationInStartTime.TimeSinceClamped <= InterpolationIn.Duration;

                    if (isInterpolatingIn)
                    {
                        interpolatingInFraction = Mathf.Clamp01(InterpolationInStartTime.TimeSinceClamped / InterpolationIn.Duration);
                    }
                }
            }

            float interpolatingOutFraction = 1f;
            bool isInterpolatingOut = false;
            bool isInterpolationOutComplete = false;
            if (!InterpolationOutStartTime.IsInfinity)
            {
                if (InterpolationOut.Duration > 0f)
                {
                    isInterpolatingOut = InterpolationOutStartTime.HasPassed;

                    if (isInterpolatingOut)
                    {
                        interpolatingOutFraction = 1f - Mathf.Clamp01(InterpolationOutStartTime.TimeSinceClamped / InterpolationOut.Duration);
                    }
                }

                isInterpolationOutComplete = InterpolationOutStartTime.TimeSince > Mathf.Max(0f, InterpolationOut.Duration);
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
                Log.Debug($"Finished interpolation out for {Util.GetGameObjectHierarchyName(gameObject)}");

                OnInterpolationOutComplete?.Invoke();
            }
        }

        [Server]
        public void SetInterpolationParameters(InterpolationParameters parameters)
        {
            InterpolationIn = parameters;
            InterpolationOut = parameters;
        }

        [Server]
        public void InterpolateOutOrDestroy()
        {
            if (InterpolationOut.Duration > 0f)
            {
                OnInterpolationOutComplete += () =>
                {
                    NetworkServer.Destroy(gameObject);
                };

                if (InterpolationOutStartTime.IsInfinity)
                {
                    InterpolationOutStartTime = Run.FixedTimeStamp.now;
                }
            }
            else
            {
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}
