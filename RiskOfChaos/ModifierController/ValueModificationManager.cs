using RoR2;
using System;
using System.Collections.Generic;
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

        public void RegisterModificationProvider(IValueModificationProvider<TValue> provider)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_modificationProviders.Add(new ModificationProviderInfo<TValue>(provider)))
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

        protected virtual TValue getModifiedValue(TValue baseValue)
        {
            foreach (ModificationProviderInfo<TValue> modificationProvider in _modificationProviders)
            {
                modificationProvider.ModificationProvider.ModifyValue(ref baseValue);
            }

            return baseValue;
        }
    }
}
