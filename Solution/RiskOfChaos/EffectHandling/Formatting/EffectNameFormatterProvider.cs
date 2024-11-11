using RiskOfChaos.ModificationController.Effect;
using System;

namespace RiskOfChaos.EffectHandling.Formatting
{
    public sealed class EffectNameFormatterProvider : IDisposable
    {
        public bool HasNameFormatterOwnership;

        bool _isDisposed;

        EffectNameFormatter _nameFormatter;
        public EffectNameFormatter NameFormatter
        {
            get
            {
                return _nameFormatter;
            }
            set
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(EffectNameFormatterProvider));

                if (ReferenceEquals(_nameFormatter, value))
                    return;

                unsubscribe(_nameFormatter);

                bool valuesChanged = _nameFormatter != value;

                _nameFormatter = value;

                subscribe(_nameFormatter);

                if (valuesChanged)
                {
                    OnNameFormatterChanged?.Invoke();
                }
            }
        }

        public event Action OnNameFormatterChanged;

        public EffectNameFormatterProvider(EffectNameFormatter nameFormatter, bool hasOwnership) : this()
        {
            NameFormatter = nameFormatter;
            HasNameFormatterOwnership = hasOwnership;
        }

        public EffectNameFormatterProvider()
        {
            EffectModificationManager.OnDurationMultiplierChanged += onEffectDurationMultiplierChanged;
        }

        void onEffectDurationMultiplierChanged()
        {
            onNameFormatterDirty();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            OnNameFormatterChanged = null;
            EffectModificationManager.OnDurationMultiplierChanged -= onEffectDurationMultiplierChanged;

            unsubscribe(NameFormatter);
        }

        void onNameFormatterDirty()
        {
            if (_isDisposed)
                return;

            OnNameFormatterChanged?.Invoke();
        }

        void subscribe(EffectNameFormatter nameFormatter)
        {
            if (nameFormatter != null)
            {
                nameFormatter.OnFormatterDirty += onNameFormatterDirty;
            }
        }

        void unsubscribe(EffectNameFormatter nameFormatter)
        {
            if (nameFormatter != null)
            {
                nameFormatter.OnFormatterDirty -= onNameFormatterDirty;

                if (HasNameFormatterOwnership && nameFormatter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
