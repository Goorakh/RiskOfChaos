﻿using RiskOfChaos.Utilities.Interpolation;
using UnityEngine;

namespace RiskOfChaos.ModifierController.Camera
{
    public struct CameraModificationData
    {
        public Vector2 RecoilMultiplier = Vector2.one;

        public float FOVMultiplier = 1f;

        public Quaternion RotationOffset = Quaternion.identity;

        public CameraModificationData()
        {
        }

        public static CameraModificationData Interpolate(in CameraModificationData a, in CameraModificationData b, float t, ValueInterpolationFunctionType type)
        {
            return new CameraModificationData
            {
                RecoilMultiplier = type.Interpolate(a.RecoilMultiplier, b.RecoilMultiplier, t),
                FOVMultiplier = type.Interpolate(a.FOVMultiplier, b.FOVMultiplier, t),
                RotationOffset = type.Interpolate(a.RotationOffset, b.RotationOffset, t)
            };
        }
    }
}
