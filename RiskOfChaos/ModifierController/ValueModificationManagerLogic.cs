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

        protected readonly HashSet<ModificationProviderInfo<TValue>> _modificationProviders = new HashSet<ModificationProviderInfo<TValue>>();

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

        public void RegisterModificationProvider(IValueModificationProvider<TValue> provider, ValueInterpolationFunctionType blendType, float valueInterpolationTime)
        {
            if (_modificationProviders.Add(new ModificationProviderInfo<TValue>(provider, blendType, valueInterpolationTime, Time.time)))
            {
                provider.OnValueDirty += MarkValueModificationsDirty;
                MarkValueModificationsDirty();
            }
        }

        public void UnregisterModificationProvider(IValueModificationProvider<TValue> provider)
        {
            if (_modificationProviders.RemoveWhere(p => p.Equals(provider)) > 0)
            {
                provider.OnValueDirty -= MarkValueModificationsDirty;
                MarkValueModificationsDirty();
            }
        }

        public void Update()
        {
            if (_modificationProviders.Any(p => p.IsInterpolating))
            {
                MarkValueModificationsDirty();
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

            UpdateValueModifications();

            OnValueModificationUpdated?.Invoke();
        }

        public void UpdateValueModifications()
        {
            _wrappedManager.UpdateValueModifications();
        }

        public TValue InterpolateValue(in TValue a, in TValue b, float t, ValueInterpolationFunctionType interpolationType)
        {
            return _wrappedManager.InterpolateValue(a, b, t, interpolationType);
        }

        public TValue GetModifiedValue(TValue baseValue)
        {
            foreach (ModificationProviderInfo<TValue> modificationProviderInfo in _modificationProviders)
            {
                if (modificationProviderInfo.IsInterpolating)
                {
                    TValue valuePreModification = baseValue.ShallowCopy();

                    modificationProviderInfo.ModificationProvider.ModifyValue(ref baseValue);

                    baseValue = InterpolateValue(valuePreModification, baseValue, Mathf.InverseLerp(0f, modificationProviderInfo.InterpolationTime, modificationProviderInfo.Age), modificationProviderInfo.InterpolationType);
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
