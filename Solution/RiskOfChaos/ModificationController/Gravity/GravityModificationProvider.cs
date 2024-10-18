using RiskOfChaos.Content;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.Gravity
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class GravityModificationProvider : NetworkBehaviour
    {
        ValueModificationController _modificationController;

        [SyncVar(hook = nameof(setGravityMultiplier))]
        float _gravityMultiplier = 1f;
        public float GravityMultiplier
        {
            get
            {
                float gravityMultiplier = _gravityMultiplier;

                if (_modificationController && _modificationController.IsInterpolating)
                {
                    gravityMultiplier = Mathf.Lerp(1f, gravityMultiplier, _modificationController.CurrentInterpolationFraction);
                }

                return gravityMultiplier;
            }
            set
            {
                _gravityMultiplier = value;
            }
        }

        [SyncVar(hook = nameof(setGravityRotation))]
        Quaternion _gravityRotation = Quaternion.identity;
        public Quaternion GravityRotation
        {
            get
            {
                Quaternion gravityRotation = _gravityRotation;

                if (_modificationController && _modificationController.IsInterpolating)
                {
                    gravityRotation = Quaternion.Slerp(Quaternion.identity, gravityRotation, _modificationController.CurrentInterpolationFraction);
                }

                return gravityRotation;
            }
            set
            {
                _gravityRotation = value;
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

        void setGravityMultiplier(float gravityMultiplier)
        {
            _gravityMultiplier = gravityMultiplier;
            onValueChanged();
        }

        void setGravityRotation(Quaternion gravityRotation)
        {
            _gravityRotation = gravityRotation;
            onValueChanged();
        }
    }
}
