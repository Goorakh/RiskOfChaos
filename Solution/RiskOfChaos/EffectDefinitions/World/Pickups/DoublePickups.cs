using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Pickups;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [ChaosTimedEffect("double_pickups", 90f)]
    public sealed class DoublePickups : MonoBehaviour
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.PickupModificationProvider;
        }

        ValueModificationController _pickupModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _pickupModificationController = Instantiate(RoCContent.NetworkedPrefabs.PickupModificationProvider).GetComponent<ValueModificationController>();

                PickupModificationProvider pickupModificationProvider = _pickupModificationController.GetComponent<PickupModificationProvider>();
                pickupModificationProvider.SpawnCountMultiplier = 2;

                NetworkServer.Spawn(_pickupModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_pickupModificationController)
            {
                _pickupModificationController.Retire();
                _pickupModificationController = null;
            }
        }
    }
}
