using RiskOfChaos.Components;
using RiskOfChaos.Utilities.Interpolation;
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

        GenericInterpolationComponent _interpolation;

        public bool IsInterpolating => _interpolation && _interpolation.IsInterpolating;

        public float CurrentInterpolationFraction => _interpolation ? _interpolation.CurrentInterpolationFraction : 1f;

        public event Action OnRetire;

        public event Action OnValuesDirty;

        void Awake()
        {
            _interpolation = GetComponent<GenericInterpolationComponent>();

            if (_interpolation)
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

            if (_interpolation)
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
            if (!_interpolation)
            {
                Log.Warning($"Cannot set interpolation parameters of {name}, missing interpolation component");
                return;
            }

            _interpolation.SetInterpolationParameters(parameters);
        }

        public void Retire()
        {
            OnRetire?.Invoke();

            if (_interpolation)
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
