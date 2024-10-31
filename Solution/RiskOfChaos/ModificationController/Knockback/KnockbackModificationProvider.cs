using RiskOfChaos.Content;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.Knockback
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class KnockbackModificationProvider : NetworkBehaviour
    {
        ValueModificationController _modificationController;

        public ValueModificationConfigBinding<float> KnockbackMultiplierConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setKnockbackMultiplier))]
        float _knockbackMultiplier = 1f;
        public float KnockbackMultiplier
        {
            get
            {
                float knockbackMultiplier = _knockbackMultiplier;

                if (_modificationController && _modificationController.IsInterpolating)
                {
                    knockbackMultiplier = Mathf.Lerp(1f, knockbackMultiplier, _modificationController.CurrentInterpolationFraction);
                }

                return knockbackMultiplier;
            }
            set
            {
                _knockbackMultiplier = value;
            }
        }

        void Awake()
        {
            _modificationController = GetComponent<ValueModificationController>();
            _modificationController.OnRetire += onRetire;

            KnockbackMultiplierConfigBinding = new ValueModificationConfigBinding<float>(setKnockbackMultiplierFromConfig);
        }

        void OnDestroy()
        {
            _modificationController.OnRetire -= onRetire;
            disposeConfigBindings();
        }

        void onRetire()
        {
            disposeConfigBindings();
        }

        void disposeConfigBindings()
        {
            KnockbackMultiplierConfigBinding?.Dispose();
        }

        void onValueChanged()
        {
            if (_modificationController)
            {
                _modificationController.InvokeOnValuesDirty();
            }
        }

        [Server]
        void setKnockbackMultiplierFromConfig(float knockbackMultiplier)
        {
            KnockbackMultiplier = knockbackMultiplier;
        }

        void setKnockbackMultiplier(float knockbackMultiplier)
        {
            _knockbackMultiplier = knockbackMultiplier;
            onValueChanged();
        }
    }
}
