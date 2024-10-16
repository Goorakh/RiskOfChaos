﻿using RiskOfChaos.Utilities.Interpolation;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Knockback
{
    [ValueModificationManager(typeof(SyncKnockbackModification))]
    public class KnockbackModificationManager : ValueModificationManager<float>
    {
        static KnockbackModificationManager _instance;
        public static KnockbackModificationManager Instance => _instance;

        SyncKnockbackModification _clientSync;

        public float TotalKnockbackMultiplier
        {
            get
            {
                return _clientSync.TotalKnockbackMultiplier;
            }
            private set
            {
                _clientSync.TotalKnockbackMultiplier = value;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _clientSync = GetComponent<SyncKnockbackModification>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SingletonHelper.Assign(ref _instance, this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SingletonHelper.Unassign(ref _instance, this);
        }

        public override float InterpolateValue(in float a, in float b, float t)
        {
            return ValueInterpolationFunctionType.Linear.Interpolate(a, b, t);
        }

        public override void UpdateValueModifications()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            TotalKnockbackMultiplier = GetModifiedValue(1f);
        }
    }
}
