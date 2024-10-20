using RiskOfChaos.Content;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.Cost
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class CostModificationProvider : NetworkBehaviour
    {
        ValueModificationController _modificationController;

        public ValueModificationConfigBinding<float> CostMultiplierConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setCostMultiplier))]
        float _costMultiplier = 1f;
        public float CostMultiplier
        {
            get
            {
                return _costMultiplier;
            }
            set
            {
                _costMultiplier = value;
            }
        }

        [SyncVar(hook = nameof(setIgnoreZeroCostRestriction))]
        bool _ignoreZeroCostRestriction;
        public bool IgnoreZeroCostRestriction
        {
            get => _ignoreZeroCostRestriction;
            set => _ignoreZeroCostRestriction = value;
        }

        bool _valuesDirty = false;

        void Awake()
        {
            _modificationController = GetComponent<ValueModificationController>();
            _modificationController.OnRetire += onRetire;

            CostMultiplierConfigBinding = new ValueModificationConfigBinding<float>(setCostMultiplierFromConfig);
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
            CostMultiplierConfigBinding?.Dispose();
        }

        void Update()
        {
            if (_valuesDirty)
            {
                _valuesDirty = false;

                if (_modificationController)
                {
                    _modificationController.InvokeOnValuesDirty();
                }
            }
        }

        void onValueChanged()
        {
            _valuesDirty = true;
        }

        [Server]
        void setCostMultiplierFromConfig(float costMultiplier)
        {
            CostMultiplier = costMultiplier;
        }

        void setCostMultiplier(float costMultiplier)
        {
            _costMultiplier = costMultiplier;
            onValueChanged();
        }

        void setIgnoreZeroCostRestriction(bool ignoreZeroCostRestriction)
        {
            _ignoreZeroCostRestriction = ignoreZeroCostRestriction;
            onValueChanged();
        }
    }
}
