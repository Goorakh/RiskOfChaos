﻿using RiskOfChaos.Networking.Wrappers;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    [DefaultExecutionOrder(-1)]
    public sealed class GenericInterpolationComponent : NetworkBehaviour
    {
        public bool DestroyOnCompleteOut = true;

        [SyncVar]
        Net_RunFixedTimeStampWrapper _interpolationInStartTimeWrapper = Run.FixedTimeStamp.positiveInfinity;

        [SyncVar]
        public InterpolationParameters InterpolationIn = InterpolationParameters.None;

        [SyncVar]
        Net_RunFixedTimeStampWrapper _interpolationOutStartTimeWrapper = Run.FixedTimeStamp.positiveInfinity;

        [SyncVar]
        public InterpolationParameters InterpolationOut = InterpolationParameters.None;

        public Run.FixedTimeStamp InterpolationInStartTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _interpolationInStartTimeWrapper;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _interpolationInStartTimeWrapper = value;
        }

        public Run.FixedTimeStamp InterpolationOutStartTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _interpolationOutStartTimeWrapper;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _interpolationOutStartTimeWrapper = value;
        }

        public bool IsInterpolating { get; private set; }

        public float CurrentInterpolationFraction { get; private set; }

        public event Action OnInterpolationChanged;

        void Start()
        {
            updateInterpolation();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (InterpolationInStartTime.isInfinity)
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

            if (isInterpolationOutComplete && DestroyOnCompleteOut && NetworkServer.active)
            {
                NetworkServer.Destroy(gameObject);
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
                InterpolationOutStartTime = Run.FixedTimeStamp.now;
            }
            else
            {
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}