using RiskOfChaos.Content;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.HoldoutZone
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class SimpleHoldoutZoneModificationProvider : NetworkBehaviour, IHoldoutZoneModificationProvider
    {
        ValueModificationController _modificationController;

        public ValueModificationConfigBinding<float> RadiusMultiplierConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setRadiusMultiplier))]
        public float RadiusMultiplier = 1f;

        public ValueModificationConfigBinding<float> ChargeRateMultiplierConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setChargeRateMultiplier))]
        public float ChargeRateMultiplier = 1f;

        void Awake()
        {
            _modificationController = GetComponent<ValueModificationController>();
            _modificationController.OnRetire += onRetire;

            RadiusMultiplierConfigBinding = new ValueModificationConfigBinding<float>(setRadiusMultiplierFromConfig);
            ChargeRateMultiplierConfigBinding = new ValueModificationConfigBinding<float>(setChargeRateMultiplierFromConfig);
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
            RadiusMultiplierConfigBinding?.Dispose();
            ChargeRateMultiplierConfigBinding?.Dispose();
        }

        void onValueChanged()
        {
            if (_modificationController)
            {
                _modificationController.InvokeOnValuesDirty();
            }
        }

        [Server]
        void setRadiusMultiplierFromConfig(float radiusMultiplier)
        {
            RadiusMultiplier = radiusMultiplier;
        }

        void setRadiusMultiplier(float radiusMultiplier)
        {
            RadiusMultiplier = radiusMultiplier;
            onValueChanged();
        }

        [Server]
        void setChargeRateMultiplierFromConfig(float chargeRateMultiplier)
        {
            ChargeRateMultiplier = chargeRateMultiplier;
        }

        void setChargeRateMultiplier(float chargeRateMultiplier)
        {
            ChargeRateMultiplier = chargeRateMultiplier;
            onValueChanged();
        }

        public HoldoutZoneModificationInfo GetHoldoutZoneModifications(HoldoutZoneController holdoutZone)
        {
            return new HoldoutZoneModificationInfo
            {
                RadiusMultiplier = RadiusMultiplier,
                ChargeRateMultiplier = ChargeRateMultiplier,
            };
        }
    }
}
