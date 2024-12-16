using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pickup;
using RoR2;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("give_random_elite_aspect", DefaultSelectionWeight = 0.6f)]
    public sealed class GiveRandomEliteAspect : NetworkBehaviour
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return EliteUtils.HasAnyRunAvailableElites;
        }

        ChaosEffectComponent _effectComponent;

        PickupIndex _aspectPickupIndex;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            EliteIndex[] eliteIndices = EliteUtils.GetRunAvailableElites(true);
            if (eliteIndices.Length > 0)
            {
                EliteIndex eliteIndex = rng.NextElementUniform(eliteIndices);
                EliteDef eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
                if (eliteDef && eliteDef.eliteEquipmentDef)
                {
                    _aspectPickupIndex = PickupCatalog.FindPickupIndex(eliteDef.eliteEquipmentDef.equipmentIndex);
                }
            }
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            if (!_aspectPickupIndex.isValid)
            {
                Log.Error($"Invalid aspect pickup def");
                return;
            }

            foreach (CharacterMaster master in CharacterMaster.readOnlyInstancesList)
            {
                if (!master || master.IsDeadAndOutOfLivesServer())
                    continue;

                PlayerCharacterMasterController playerMasterController = master.playerCharacterMasterController;
                if (playerMasterController && !playerMasterController.isConnected)
                    continue;

                if (!master.IsPlayerOrPlayerAlly())
                    continue;

                try
                {
                    if (master.inventory.TryGrant(_aspectPickupIndex, InventoryExtensions.EquipmentReplacementRule.DropExisting))
                    {
                        if (playerMasterController)
                        {
                            PickupUtils.QueuePickupMessage(master, _aspectPickupIndex);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix($"Failed to give aspect pickup to {Util.GetBestMasterName(master)}: {e}");
                }
            }
        }
    }
}
