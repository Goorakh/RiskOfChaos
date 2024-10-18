using RiskOfChaos.Content;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.Knockback
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class KnockbackModificationProvider : NetworkBehaviour
    {
        ValueModificationController _modificationController;

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
        }

        void onValueChanged()
        {
            if (_modificationController)
            {
                _modificationController.InvokeOnValuesDirty();
            }
        }

        void setKnockbackMultiplier(float knockbackMultiplier)
        {
            _knockbackMultiplier = knockbackMultiplier;
            onValueChanged();
        }
    }
}
