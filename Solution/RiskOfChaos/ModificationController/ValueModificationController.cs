using RiskOfChaos.Components;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RiskOfChaos.ModificationController
{
    public sealed class ValueModificationController : MonoBehaviour
    {
        static readonly List<ValueModificationController> _instances = [];
        public static readonly ReadOnlyCollection<ValueModificationController> Instances = new ReadOnlyCollection<ValueModificationController>(_instances);

        public delegate void ValueModificationControllerEventHandler(ValueModificationController modificationController);
        public static event ValueModificationControllerEventHandler OnModificationControllerStartGlobal;
        public static event ValueModificationControllerEventHandler OnModificationControllerEndGlobal;

        IInterpolationProvider _interpolation;

        public bool IsInterpolating => _interpolation != null && _interpolation.IsInterpolating;

        public float CurrentInterpolationFraction => _interpolation != null ? _interpolation.CurrentInterpolationFraction : 1f;

        public bool IsRetired { get; private set; }

        public event Action OnRetire;

        public event Action OnValuesDirty;

        void Awake()
        {
            _interpolation = GetComponent<IInterpolationProvider>();

            if (_interpolation != null)
            {
                _interpolation.OnInterpolationChanged += InvokeOnValuesDirty;
            }
        }

        void Start()
        {
            _instances.Add(this);
            OnModificationControllerStartGlobal?.Invoke(this);
        }

        void OnDestroy()
        {
            _instances.Remove(this);
            OnModificationControllerEndGlobal?.Invoke(this);

            if (_interpolation != null)
            {
                _interpolation.OnInterpolationChanged -= InvokeOnValuesDirty;
            }
        }

        public void InvokeOnValuesDirty()
        {
            OnValuesDirty?.Invoke();
        }

        public void SetInterpolationParameters(InterpolationParameters parameters)
        {
            if (_interpolation == null)
            {
                Log.Warning($"Cannot set interpolation parameters of {Util.GetGameObjectHierarchyName(gameObject)}, missing interpolation component");
                return;
            }

            _interpolation.SetInterpolationParameters(parameters);
        }

        public void Retire()
        {
            IsRetired = true;

            OnRetire?.Invoke();

            if (_interpolation != null)
            {
                _interpolation.InterpolateOutOrDestroy();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
