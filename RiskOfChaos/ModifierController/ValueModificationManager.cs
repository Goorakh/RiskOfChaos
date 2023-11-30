using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController
{
    public abstract class ValueModificationManager<TValue> : MonoBehaviour, IValueModificationManager<TValue>
    {
#if DEBUG
        float _lastModificationDirtyLogAttemptTime = float.NegativeInfinity;
#endif

        protected readonly HashSet<ModificationProviderInfo<TValue>> _modificationProviders = new HashSet<ModificationProviderInfo<TValue>>();

        public event Action OnValueModificationUpdated;

        public bool AnyModificationActive { get; private set; }

        protected virtual float modificationInterpolationTime => 1f;

        bool _modificationProvidersDirty;

        protected void onModificationProviderDirty()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_modificationProvidersDirty)
                return;

#if DEBUG
            if (Time.unscaledTime >= _lastModificationDirtyLogAttemptTime + 0.25f)
                Log.Debug_NoCallerPrefix($"{name} modification marked dirty");

            _lastModificationDirtyLogAttemptTime = Time.unscaledTime;
#endif

            RoR2Application.onNextUpdate += updateValueModifiers;

            _modificationProvidersDirty = true;
        }

        public void RegisterModificationProvider(IValueModificationProvider<TValue> provider, ValueInterpolationFunctionType valueInterpolationType = ValueInterpolationFunctionType.Snap)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_modificationProviders.Add(new ModificationProviderInfo<TValue>(provider, valueInterpolationType)))
            {
                provider.OnValueDirty += onModificationProviderDirty;
                onModificationProviderDirty();
            }
        }

        public void UnregisterModificationProvider(IValueModificationProvider<TValue> provider)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_modificationProviders.RemoveWhere(p => p.Equals(provider)) > 0)
            {
                provider.OnValueDirty -= onModificationProviderDirty;
                onModificationProviderDirty();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (_modificationProviders.Any(p => p.Age <= modificationInterpolationTime))
            {
                onModificationProviderDirty();
            }
        }

        void updateValueModifiers()
        {
            _modificationProvidersDirty = false;

            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            AnyModificationActive = _modificationProviders.Count > 0;

            updateValueModifications();

            OnValueModificationUpdated?.Invoke();
        }

        protected abstract void updateValueModifications();

        protected abstract TValue interpolateValue(in TValue a, in TValue b, float t, ValueInterpolationFunctionType interpolationType);

        protected virtual TValue getModifiedValue(TValue baseValue)
        {
            foreach (ModificationProviderInfo<TValue> modificationProvider in _modificationProviders)
            {
                TValue valuePreModification = baseValue.ShallowCopy();

                modificationProvider.ModificationProvider.ModifyValue(ref baseValue);

                baseValue = interpolateValue(valuePreModification, baseValue, Mathf.InverseLerp(0f, modificationInterpolationTime, modificationProvider.Age), modificationProvider.InterpolationType);
            }

            return baseValue;
        }
    }
}
