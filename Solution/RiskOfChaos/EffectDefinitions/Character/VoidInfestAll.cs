using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.CharacterAI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("void_infest_all", DefaultSelectionWeight = 0.6f)]
    [EffectConfigBackwardsCompatibility("Effect: Touch Void")]
    public sealed class VoidInfestAll : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<bool> _excludeAllAllies =
            ConfigFactory<bool>.CreateConfig("Exclude Player Allies", false)
                               .Description("Excludes all player allies from being voidtouched")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        [EffectConfig]
        static readonly ConfigHolder<bool> _excludeDrones =
            ConfigFactory<bool>.CreateConfig("Exclude Drones", true)
                               .Description("Excludes all drones from being voidtouched")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        static bool canInfestBody(CharacterBody body)
        {
            if (!body || body.isPlayerControlled)
                return false;

            // No inventory: Can't carry aspect, effect will do nothing
            if (!body.inventory)
                return false;

            if (!body.healthComponent || !body.healthComponent.alive)
                return false;

            if (_excludeAllAllies.Value && body.IsPlayerOrPlayerAlly())
                return false;

            if (_excludeDrones.Value && (body.bodyFlags & CharacterBody.BodyFlags.Drone) != 0)
                return false;

            return true;
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return ExpansionUtils.DLC1Enabled && (!context.IsNow || CharacterBody.readOnlyInstancesList.Any(canInfestBody));
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            using (ListPool<BaseAI>.RentCollection(out List<BaseAI> aiToReset))
            {
                aiToReset.EnsureCapacity(CharacterMaster.readOnlyInstancesList.Count);

                foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
                {
                    if (!canInfestBody(body))
                        continue;

                    CharacterMaster master = body.master;

                    body.teamComponent.teamIndex = TeamIndex.Void;

                    Inventory inventory = body.inventory;
                    if (inventory)
                    {
                        inventory.SetEquipmentIndex(DLC1Content.Equipment.EliteVoidEquipment.equipmentIndex, false);
                    }

                    if (master)
                    {
                        master.teamIndex = TeamIndex.Void;

                        aiToReset.AddRange(master.AiComponents);

                        // Make sure void infested allies don't stay until the next stage
                        master.gameObject.SetDontDestroyOnLoad(false);
                    }

                    if (EntityStates.VoidInfestor.Infest.successfulInfestEffectPrefab)
                    {
                        EffectManager.SimpleImpactEffect(EntityStates.VoidInfestor.Infest.successfulInfestEffectPrefab, body.corePosition, Vector3.up, true);
                    }
                }

                foreach (BaseAI baseAI in aiToReset)
                {
                    baseAI.enemyAttention = 0f;
                    baseAI.currentEnemy.Reset();
                    baseAI.ForceAcquireNearestEnemyIfNoCurrentEnemy();
                }
            }
        }
    }
}
