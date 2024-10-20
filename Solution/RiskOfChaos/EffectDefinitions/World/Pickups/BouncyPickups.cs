using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Pickups;
using RiskOfOptions.OptionConfigs;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [ChaosTimedEffect("bouncy_pickups", 60f, AllowDuplicates = true)]
    public sealed class BouncyPickups : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _bounceCount =
            ConfigFactory<int>.CreateConfig("Bounce Count", 2)
                              .Description("How many times items should bounce before settling")
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

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
                pickupModificationProvider.BounceCountConfigBinding.BindToConfig(_bounceCount);

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
