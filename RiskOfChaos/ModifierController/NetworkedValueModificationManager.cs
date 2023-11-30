using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController
{
    public abstract class NetworkedValueModificationManager<TValue> : NetworkBehaviour, IValueModificationManager<TValue>
    {
#if DEBUG
        float _lastModificationDirtyLogAttemptTime = float.NegativeInfinity;
#endif

        protected readonly HashSet<ModificationProviderInfo<TValue>> _modificationProviders = new HashSet<ModificationProviderInfo<TValue>>();

        const uint ANY_MODIFICATION_ACTIVE_DIRTY_BIT = 1 << 0;

        public event Action OnValueModificationUpdated;

        bool _anyModificationActive;
        public bool AnyModificationActive
        {
            get
            {
                return _anyModificationActive;
            }

            [param: In]
            private set
            {
                if (NetworkServer.localClientActive && !syncVarHookGuard)
                {
                    syncVarHookGuard = true;
                    syncAnyModificationActive(value);
                    syncVarHookGuard = false;
                }

                SetSyncVar(value, ref _anyModificationActive, ANY_MODIFICATION_ACTIVE_DIRTY_BIT);
            }
        }

        protected virtual float modificationInterpolationTime => 1f;

        public override void OnStartClient()
        {
            base.OnStartClient();

            syncAnyModificationActive(_anyModificationActive);
        }

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

        public sealed override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            uint dirtyBits = syncVarDirtyBits;
            if (!initialState)
            {
                writer.WritePackedUInt32(dirtyBits);
            }

            return serialize(writer, initialState, dirtyBits);
        }

        protected virtual bool serialize(NetworkWriter writer, bool initialState, uint dirtyBits)
        {
            if (initialState)
            {
                writer.Write(_anyModificationActive);
                return true;
            }

            bool anythingWritten = false;

            if ((dirtyBits & ANY_MODIFICATION_ACTIVE_DIRTY_BIT) != 0)
            {
                writer.Write(_anyModificationActive);
                anythingWritten = true;
            }

            return anythingWritten;
        }

        public sealed override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            deserialize(reader, initialState, initialState ? ~0b0U : reader.ReadPackedUInt32());

            if (!NetworkServer.active)
            {
                OnValueModificationUpdated?.Invoke();
            }
        }

        protected virtual void deserialize(NetworkReader reader, bool initialState, uint dirtyBits)
        {
            if (initialState)
            {
                _anyModificationActive = reader.ReadBoolean();
                return;
            }

            if ((dirtyBits & ANY_MODIFICATION_ACTIVE_DIRTY_BIT) != 0)
            {
                syncAnyModificationActive(reader.ReadBoolean());
            }
        }
    }
}
