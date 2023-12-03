using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController
{
    public class ValueModificationManagerLogic<TValue> : IValueModificationManager<TValue>
    {
        readonly IValueModificationManager<TValue> _wrappedManager;

#if DEBUG
        float _lastModificationDirtyLogAttemptTime = float.NegativeInfinity;
#endif

        protected readonly List<ModificationProviderInfo<TValue>> _modificationProviders = new List<ModificationProviderInfo<TValue>>();

        bool _anyModificationActive;
        public bool AnyModificationActive
        {
            get
            {
                return _anyModificationActive;
            }
            private set
            {
                if (_anyModificationActive != value)
                {
                    _anyModificationActive = value;
                    OnAnyModificationActiveChanged?.Invoke(value);
                }
            }
        }

        public void NetworkSetAnyModificationActive(bool value)
        {
            AnyModificationActive = value;
        }

        public delegate void AnyModificationActiveChangedDelegate(bool newValue);
        public event AnyModificationActiveChangedDelegate OnAnyModificationActiveChanged;

        public event Action OnValueModificationUpdated;

        bool _modificationProvidersDirty;

        public ValueModificationManagerLogic(IValueModificationManager<TValue> wrappedManager)
        {
            _wrappedManager = wrappedManager;
        }

        public void MarkValueModificationsDirty()
        {
            if (_modificationProvidersDirty)
                return;

#if DEBUG
            if (Time.unscaledTime >= _lastModificationDirtyLogAttemptTime + 0.25f)
                Log.Debug_NoCallerPrefix($"{_wrappedManager} modification marked dirty");

            _lastModificationDirtyLogAttemptTime = Time.unscaledTime;
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

        public void RegisterModificationProvider(IValueModificationProvider<TValue> provider, ValueInterpolationFunctionType blendType, float valueInterpolationTime)
        {
            ModificationProviderInfo<TValue> providerInfo = new ModificationProviderInfo<TValue>(provider);
            _modificationProviders.Add(providerInfo);

            provider.OnValueDirty += MarkValueModificationsDirty;
            MarkValueModificationsDirty();

            if (valueInterpolationTime > 0f)
            {
                providerInfo.StartInterpolatingIn(blendType, valueInterpolationTime);
            }
        }

        public void UnregisterModificationProvider(IValueModificationProvider<TValue> provider, ValueInterpolationFunctionType blendType, float valueInterpolationTime)
        {
            for (int i = _modificationProviders.Count - 1; i >= 0; i--)
            {
                if (_modificationProviders[i].Equals(provider))
                {
                    _modificationProviders[i].ModificationProvider.OnValueDirty -= MarkValueModificationsDirty;
                    MarkValueModificationsDirty();

                    if (valueInterpolationTime > 0f)
                    {
                        _modificationProviders[i].StartInterpolatingOut(blendType, valueInterpolationTime);
                    }
                    else
                    {
                        _modificationProviders.RemoveAt(i);
                    }
                }
            }
        }

        public void Update()
        {
            bool modificationsDirty = false;

            for (int i = _modificationProviders.Count - 1; i >= 0; i--)
            {
                ModificationProviderInfo<TValue> provider = _modificationProviders[i];
                if (provider.InterpolationState.IsInterpolating)
                {
                    modificationsDirty = true;
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

        public void UpdateValueModifications()
        {
            _wrappedManager.UpdateValueModifications();
        }

        public TValue InterpolateValue(in TValue a, in TValue b, float t)
        {
            return _wrappedManager.InterpolateValue(a, b, t);
        }

        public TValue GetModifiedValue(TValue baseValue)
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
