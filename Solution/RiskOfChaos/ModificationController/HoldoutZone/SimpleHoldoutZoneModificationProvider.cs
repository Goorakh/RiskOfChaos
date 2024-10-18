using RiskOfChaos.Content;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.HoldoutZone
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class SimpleHoldoutZoneModificationProvider : NetworkBehaviour, IHoldoutZoneModificationProvider
    {
        ValueModificationController _modificationController;

        [SyncVar(hook = nameof(setRadiusMultiplier))]
        public float RadiusMultiplier = 1f;

        [SyncVar(hook = nameof(setChargeRateMultiplier))]
        public float ChargeRateMultiplier = 1f;

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

        void setRadiusMultiplier(float radiusMultiplier)
        {
            RadiusMultiplier = radiusMultiplier;
            onValueChanged();
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
