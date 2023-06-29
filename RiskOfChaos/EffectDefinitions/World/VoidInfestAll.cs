using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("void_infest_all", DefaultSelectionWeight = 0.6f, EffectWeightReductionPercentagePerActivation = 20f)]
    public sealed class VoidInfestAll : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return ExpansionUtils.DLC1Enabled;
        }

        public override void OnStart()
        {
            EquipmentIndex voidEliteEquipmentIndex = DLC1Content.Elites.Void.eliteEquipmentDef.equipmentIndex;

            CharacterBody.readOnlyInstancesList.TryDo(body =>
            {
                if (!body || body.isPlayerControlled)
                    return;

                HealthComponent healthComponent = body.healthComponent;
                if (!healthComponent.alive)
                    return;

                CharacterMaster master = body.master;
                if (!master)
                    return;

                master.teamIndex = TeamIndex.Void;
                body.teamComponent.teamIndex = TeamIndex.Void;

                Inventory inventory = body.inventory;
                if (inventory)
                {
                    inventory.SetEquipmentIndex(voidEliteEquipmentIndex);
                }

                if (master.TryGetComponent<BaseAI>(out BaseAI baseAI))
                {
                    baseAI.enemyAttention = 0f;
                    baseAI.ForceAcquireNearestEnemyIfNoCurrentEnemy();
                }

                // Make sure void infested allies don't stay until the next stage
                master.gameObject.SetDontDestroyOnLoad(false);

                if (EntityStates.VoidInfestor.Infest.successfulInfestEffectPrefab)
                {
                    EffectManager.SimpleImpactEffect(EntityStates.VoidInfestor.Infest.successfulInfestEffectPrefab, body.corePosition, Vector3.up, true);
                }

                BossUtils.TryRefreshBossTitleFor(body);
            }, FormatUtils.GetBestBodyName);
        }
    }
}
