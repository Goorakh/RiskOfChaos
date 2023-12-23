using System;
using RiskOfChaos.Utilities.Interpolation;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController
{
    public abstract class NetworkedValueModificationManager<TValue> : NetworkBehaviour, IValueModificationManager<TValue>
    {
        ValueModificationManagerLogic<TValue> _logic;

        const uint ANY_MODIFICATION_ACTIVE_DIRTY_BIT = 1 << 0;

        public event Action OnValueModificationUpdated;

        bool _ignoreModificationActiveChanged;
        public bool AnyModificationActive
        {
            get
            {
                return _logic.AnyModificationActive;
            }
            private set
            {
                _ignoreModificationActiveChanged = true;
                try
                {
                    _logic.NetworkSetAnyModificationActive(value);
                }
                finally
                {
                    _ignoreModificationActiveChanged = false;
                }
            }
        }

        protected virtual void Awake()
        {
            _logic = new ValueModificationManagerLogic<TValue>(this);
        }

        protected virtual void OnEnable()
        {
            _logic.OnValueModificationUpdated += _logic_OnValueModificationUpdated;

            if (NetworkServer.active)
            {
                _logic.OnAnyModificationActiveChanged += _logic_OnAnyModificationActiveChanged;
            }
        }

        protected virtual void OnDisable()
        {
            _logic.OnValueModificationUpdated -= _logic_OnValueModificationUpdated;
            _logic.OnAnyModificationActiveChanged -= _logic_OnAnyModificationActiveChanged;

            ClearAllModificationProviders();
        }

        public void ClearAllModificationProviders()
        {
            _logic.ClearAllModificationProviders();
        }

        void _logic_OnValueModificationUpdated()
        {
            OnValueModificationUpdated?.Invoke();
        }

        void _logic_OnAnyModificationActiveChanged(bool newValue)
        {
            if (!_ignoreModificationActiveChanged)
            {
                SetDirtyBit(ANY_MODIFICATION_ACTIVE_DIRTY_BIT);
            }
        }

        public void RegisterModificationProvider(IValueModificationProvider<TValue> provider)
        {
            RegisterModificationProvider(provider, ValueInterpolationFunctionType.Snap, 0f);
        }

        public InterpolationState RegisterModificationProvider(IValueModificationProvider<TValue> provider, ValueInterpolationFunctionType blendType, float valueInterpolationTime)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return null;
            }

            return _logic.RegisterModificationProvider(provider, blendType, valueInterpolationTime);
        }

        public void UnregisterModificationProvider(IValueModificationProvider<TValue> provider)
        {
            _logic.UnregisterModificationProvider(provider, ValueInterpolationFunctionType.Snap, 0f);
        }

        public InterpolationState UnregisterModificationProvider(IValueModificationProvider<TValue> provider, ValueInterpolationFunctionType blendType, float valueInterpolationTime)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return null;
            }

            return _logic.UnregisterModificationProvider(provider, blendType, valueInterpolationTime);
        }

        protected virtual void FixedUpdate()
        {
            _logic.Update();
        }

        public void MarkValueModificationsDirty()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            _logic.MarkValueModificationsDirty();
        }

        public abstract void UpdateValueModifications();

        public abstract TValue InterpolateValue(in TValue a, in TValue b, float t);

        public virtual TValue GetModifiedValue(TValue baseValue)
        {
            return _logic.GetModifiedValue(baseValue);
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
                writer.Write(AnyModificationActive);
                return true;
            }

            bool anythingWritten = false;

            if ((dirtyBits & ANY_MODIFICATION_ACTIVE_DIRTY_BIT) != 0)
            {
                writer.Write(AnyModificationActive);
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
                AnyModificationActive = reader.ReadBoolean();
                return;
            }

            if ((dirtyBits & ANY_MODIFICATION_ACTIVE_DIRTY_BIT) != 0)
            {
                AnyModificationActive = reader.ReadBoolean();
            }
        }
    }
}
