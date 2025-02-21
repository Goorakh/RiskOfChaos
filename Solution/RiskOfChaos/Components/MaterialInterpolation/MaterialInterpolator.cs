﻿using System;
using UnityEngine;

namespace RiskOfChaos.Components.MaterialInterpolation
{
    public sealed class MaterialInterpolator : MonoBehaviour, IMaterialProvider
    {
        IInterpolationProvider _interpolation;

        IMaterialPropertyInterpolator[] _propertyInterpolators;

        Material _sharedMaterial;

        public Material MaterialInstance { get; private set; }

        public event Action OnMaterialPropertiesChanged;

        Material IMaterialProvider.Material
        {
            get => MaterialInstance;
            set => SetMaterial(value);
        }

        event Action IMaterialProvider.OnPropertiesChanged
        {
            add => OnMaterialPropertiesChanged += value;
            remove => OnMaterialPropertiesChanged -= value;
        }

        void Awake()
        {
            _propertyInterpolators = GetComponents<IMaterialPropertyInterpolator>();

            _interpolation = GetComponent<IInterpolationProvider>();

            if (_interpolation == null)
            {
                Log.Error("Missing interpolation provider");
                enabled = false;
                return;
            }

            _interpolation.OnInterpolationChanged += updateMaterialProperties;
        }

        void OnDestroy()
        {
            SetMaterial(null);
        }

        public void SetMaterial(Material mat)
        {
            if (mat && (mat == _sharedMaterial || mat == MaterialInstance))
                return;

            if (MaterialInstance)
            {
                Destroy(MaterialInstance);
                MaterialInstance = null;
            }

            _sharedMaterial = mat;
            MaterialInstance = _sharedMaterial ? new Material(_sharedMaterial) : null;

            if (MaterialInstance)
            {
                updateMaterialProperties();
            }
        }

        void updateMaterialProperties()
        {
            if (!MaterialInstance)
                return;

            float interpolationFraction = _interpolation.CurrentInterpolationFraction;

            foreach (IMaterialPropertyInterpolator propertyInterpolator in _propertyInterpolators)
            {
                propertyInterpolator.SetValues(MaterialInstance, interpolationFraction);
            }

            OnMaterialPropertiesChanged?.Invoke();
        }
    }
}
