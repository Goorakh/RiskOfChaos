using System;
using UnityEngine;

namespace RiskOfChaos.ModifierController
{
    public abstract class ValueModificationManager<TValue> : MonoBehaviour, IValueModificationManager<TValue>
    {
        ValueModificationManagerLogic<TValue> _logic;

        public event Action OnValueModificationUpdated;

        public bool AnyModificationActive => _logic.AnyModificationActive;

        protected virtual void Awake()
        {
            _logic = new ValueModificationManagerLogic<TValue>(this);
        }

        protected virtual void OnEnable()
        {
            _logic.OnValueModificationUpdated += _logic_OnValueModificationUpdated;
        }

        protected virtual void OnDisable()
        {
            _logic.OnValueModificationUpdated -= _logic_OnValueModificationUpdated;
        }

        void _logic_OnValueModificationUpdated()
        {
            OnValueModificationUpdated?.Invoke();
        }

        public void RegisterModificationProvider(IValueModificationProvider<TValue> provider)
        {
            RegisterModificationProvider(provider, ValueInterpolationFunctionType.Snap, 0f);
        }

        public void RegisterModificationProvider(IValueModificationProvider<TValue> provider, ValueInterpolationFunctionType blendType, float valueInterpolationTime)
        {
            _logic.RegisterModificationProvider(provider, blendType, valueInterpolationTime);
        }

        public void UnregisterModificationProvider(IValueModificationProvider<TValue> provider)
        {
            _logic.UnregisterModificationProvider(provider);
        }

        protected virtual void FixedUpdate()
        {
            _logic.Update();
        }

        public void MarkValueModificationsDirty()
        {
            _logic.MarkValueModificationsDirty();
        }

        public abstract void UpdateValueModifications();

        public abstract TValue InterpolateValue(in TValue a, in TValue b, float t, ValueInterpolationFunctionType interpolationType);

        public virtual TValue GetModifiedValue(TValue baseValue)
        {
            return _logic.GetModifiedValue(baseValue);
        }
    }
}
