using RiskOfChaos.Collections.CatalogIndex;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    /*
    [ChaosEffect("spawn_healing_core")]
    public sealed class SpawnHealingCore : MonoBehaviour
    {
        static GameObject _healingCoreMasterPrefab;

        static readonly BodyIndexCollection _spawnOnBlacklist = new BodyIndexCollection([
            "AffixEarthHealerBody",
            "BirdsharkBody"
        ]);

        [SystemInitializer(typeof(MasterCatalog))]
        static void Init()
        {
            _healingCoreMasterPrefab = MasterCatalog.FindMasterPrefab("AffixEarthHealerMaster");
            if (!_healingCoreMasterPrefab)
            {
                Log.Error("Failed to find healing core master prefab");
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return ExpansionUtils.DLC1Enabled && _healingCoreMasterPrefab && ExpansionUtils.IsObjectExpansionAvailable(_healingCoreMasterPrefab);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            List<CharacterBody> spawnTargets = new List<CharacterBody>(CharacterBody.readOnlyInstancesList.Count);

            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (body && !_spawnOnBlacklist.Contains(body.bodyIndex))
                {
                    spawnTargets.Add(body);
                }
            }

            foreach (CharacterBody body in spawnTargets)
            {
                Vector2 randomSpawnOffset = UnityEngine.Random.insideUnitCircle * 0.75f;

                CharacterMaster healingCoreMaster = new MasterSummon
                {
                    masterPrefab = _healingCoreMasterPrefab,
                    position = body.corePosition + new Vector3(randomSpawnOffset.x, 0.5f, randomSpawnOffset.y),
                    rotation = Quaternion.identity,
                    summonerBodyObject = body.gameObject,
                    ignoreTeamMemberLimit = true
                }.Perform();

                if (healingCoreMaster)
                {
                    Inventory healingCoreInventory = healingCoreMaster.inventory;
                    if (healingCoreInventory)
                    {
                        int damageBoost = 0;
                        if (body && body.level > 1)
                        {
                            damageBoost = Mathf.Max(1, Mathf.FloorToInt((body.level - 1) * 0.8f));
                        }

                        if (damageBoost > 0)
                        {
                            healingCoreInventory.GiveItem(RoR2Content.Items.BoostDamage, damageBoost);
                        }
                    }
                }
            }
        }
    }
    */
}
