using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pickup;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("give_random_elite_aspect", DefaultSelectionWeight = 0.6f)]
    public sealed class GiveRandomEliteAspect : NetworkBehaviour
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return EliteUtils.HasAnyAvailableEliteEquipments;
        }

        ChaosEffectComponent _effectComponent;

        PickupDef _aspectPickupDef;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _aspectPickupDef = PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(EliteUtils.GetRandomEliteEquipmentIndex(rng)));
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                if (_aspectPickupDef != null)
                {
                    PlayerUtils.GetAllPlayerMasters(true).TryDo(playerMaster =>
                    {
                        if (playerMaster.inventory.TryGrant(_aspectPickupDef, InventoryExtensions.ItemReplacementRule.DropExisting))
                        {
                            PickupUtils.QueuePickupMessage(playerMaster, _aspectPickupDef.pickupIndex);
                        }
                    }, Util.GetBestMasterName);
                }
                else
                {
                    Log.Error($"Invalid aspect pickup def");
                }
            }
        }
    }
}
