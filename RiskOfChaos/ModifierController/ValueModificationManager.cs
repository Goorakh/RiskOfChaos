using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController
{
    public abstract class ValueModificationManager<TModificationProvider, TValue> : MonoBehaviour, IValueModificationManager<TModificationProvider, TValue> where TModificationProvider : IValueModificationProvider<TValue>
    {
#if DEBUG
        float _lastModificationDirtyLogAttemptTime = float.NegativeInfinity;
#endif

        protected readonly HashSet<TModificationProvider> _modificationProviders = new HashSet<TModificationProvider>();

        public event Action OnValueModificationUpdated;

        public bool AnyModificationActive { get; private set; }

        protected virtual void syncAnyModificationActive(bool active)
        {
            AnyModificationActive = active;
        }

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

        public void RegisterModificationProvider(TModificationProvider provider)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_modificationProviders.Add(provider))
            {
                provider.OnValueDirty += onModificationProviderDirty;
                onModificationProviderDirty();
            }
        }

        public void UnregisterModificationProvider(TModificationProvider provider)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_modificationProviders.Remove(provider))
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
            foreach (TModificationProvider modificationProvider in _modificationProviders)
            {
                modificationProvider.ModifyValue(ref baseValue);
            }

            return baseValue;
        }
    }
}
