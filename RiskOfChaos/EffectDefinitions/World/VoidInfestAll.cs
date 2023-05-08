using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (!body || body.isPlayerControlled)
                    continue;

                HealthComponent healthComponent = body.healthComponent;
                if (!healthComponent.alive)
                    continue;

                CharacterMaster master = body.master;
                if (!master)
                    continue;

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
                if (Util.IsDontDestroyOnLoad(master.gameObject))
                {
                    SceneManager.MoveGameObjectToScene(master.gameObject, SceneManager.GetActiveScene());
                }

                if (EntityStates.VoidInfestor.Infest.successfulInfestEffectPrefab)
                {
                    EffectManager.SimpleImpactEffect(EntityStates.VoidInfestor.Infest.successfulInfestEffectPrefab, body.corePosition, Vector3.up, true);
                }

                BossUtils.TryRefreshBossTitleFor(body);
            }
        }
    }
}
