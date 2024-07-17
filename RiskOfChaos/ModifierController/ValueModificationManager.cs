using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.ModifierController
{
    public abstract class ValueModificationManager<TValue> : MonoBehaviour
    {
#if DEBUG
        float _lastModificationDirtyLogAttemptTime = float.NegativeInfinity;
#endif

        protected readonly List<ModificationProviderInfo<TValue>> _modificationProviders = [];

        bool _anyModificationActive;
        public virtual bool AnyModificationActive
        {
            get
            {
                return _anyModificationActive;
            }
            private set
            {
                if (_anyModificationActive == value)
                    return;
                
                _anyModificationActive = value;
                OnAnyModificationActiveChanged?.Invoke(_anyModificationActive);
            }
        }

        public delegate void AnyModificationActiveChangedDelegate(bool newValue);
        public event AnyModificationActiveChangedDelegate OnAnyModificationActiveChanged;

        public event Action OnValueModificationUpdated;

        bool _modificationProvidersDirty;

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
            ClearAllModificationProviders();
        }

        public void MarkValueModificationsDirty()
        {
            if (_modificationProvidersDirty)
                return;

#if DEBUG
            if (Time.unscaledTime >= _lastModificationDirtyLogAttemptTime + 0.25f)
            {
                Log.Debug_NoCallerPrefix($"{this} modification marked dirty");

                _lastModificationDirtyLogAttemptTime = Time.unscaledTime;
            }
#endif

            RoR2Application.onNextUpdate += updateValueModifiers;

            _modificationProvidersDirty = true;
        }

        public void ClearAllModificationProviders()
        {
            if (AnyModificationActive)
            {
                _modificationProviders.Clear();
                MarkValueModificationsDirty();
            }
        }

        public InterpolationState RegisterModificationProvider(IValueModificationProvider<TValue> provider, ValueInterpolationFunctionType blendType = ValueInterpolationFunctionType.Snap, float valueInterpolationTime = 0f)
        {
            ModificationProviderInfo<TValue> providerInfo = new ModificationProviderInfo<TValue>(provider);
            _modificationProviders.Add(providerInfo);

            provider.OnValueDirty += MarkValueModificationsDirty;
            MarkValueModificationsDirty();

            if (valueInterpolationTime > 0f)
            {
                providerInfo.StartInterpolatingIn(blendType, valueInterpolationTime);
            }

            return providerInfo.InterpolationState;
        }

        public InterpolationState UnregisterModificationProvider(IValueModificationProvider<TValue> provider, ValueInterpolationFunctionType blendType = ValueInterpolationFunctionType.Snap, float valueInterpolationTime = 0f)
        {
            InterpolationState result = null;

            for (int i = _modificationProviders.Count - 1; i >= 0; i--)
            {
                ModificationProviderInfo<TValue> modificationProvider = _modificationProviders[i];
                if (modificationProvider.Equals(provider))
                {
                    modificationProvider.ModificationProvider.OnValueDirty -= MarkValueModificationsDirty;
                    MarkValueModificationsDirty();

                    if (valueInterpolationTime > 0f)
                    {
                        modificationProvider.StartInterpolatingOut(blendType, valueInterpolationTime);
                        result = modificationProvider.InterpolationState;
                    }
                    else
                    {
                        _modificationProviders.RemoveAt(i);
                    }
                }
            }

            return result;
        }

        protected virtual void FixedUpdate()
        {
            bool modificationsDirty = false;

            for (int i = _modificationProviders.Count - 1; i >= 0; i--)
            {
                ModificationProviderInfo<TValue> provider = _modificationProviders[i];
                if (provider.InterpolationState.IsInterpolating)
                {
                    modificationsDirty = true;
                    provider.OnInterpolationUpdate();
                }
                else if (provider.InterpolationDirection > ModificationProviderInterpolationDirection.None) // Interpolation has finished
                {
#if DEBUG
                    Log.Debug($"{provider.ModificationProvider} value interpolation finished ({provider.InterpolationDirection})");
#endif

                    // If out interpolation finished, the modification is done and should be removed
                    if (provider.InterpolationDirection == ModificationProviderInterpolationDirection.Out)
                    {
                        _modificationProviders.RemoveAt(i);
                    }

                    provider.OnInterpolationFinished();

                    modificationsDirty = true;
                }
            }

            if (modificationsDirty)
            {
                MarkValueModificationsDirty();
            }
        }

        void updateValueModifiers()
        {
            _modificationProvidersDirty = false;

            AnyModificationActive = _modificationProviders.Count > 0;

            UpdateValueModifications();

            OnValueModificationUpdated?.Invoke();
        }

        public abstract void UpdateValueModifications();

        public abstract TValue InterpolateValue(in TValue a, in TValue b, float t);

        public virtual TValue GetModifiedValue(TValue baseValue)
        {
            foreach (ModificationProviderInfo<TValue> modificationProviderInfo in _modificationProviders)
            {
                if (modificationProviderInfo.InterpolationState.IsInterpolating)
                {
                    TValue valuePreModification = baseValue.ShallowCopy();

                    modificationProviderInfo.ModificationProvider.ModifyValue(ref baseValue);

                    baseValue = InterpolateValue(valuePreModification, baseValue, modificationProviderInfo.InterpolationState.CurrentFraction);
                }
                else
                {
                    modificationProviderInfo.ModificationProvider.ModifyValue(ref baseValue);
                }
            }

            return baseValue;
        }
    }
}
