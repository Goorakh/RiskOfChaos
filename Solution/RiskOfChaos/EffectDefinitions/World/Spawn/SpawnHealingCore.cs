﻿using RiskOfChaos.Collections.CatalogIndex;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_healing_core")]
    public sealed class SpawnHealingCore : MonoBehaviour
    {
        static GameObject _healingCoreMasterPrefab;

        static readonly BodyIndexCollection _spawnOnBlacklist = new BodyIndexCollection([
            "AffixEarthHealerBody",
            "BirdsharkBody"
        ]);

        [SystemInitializer]
        static void Init()
        {
            Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/EliteEarth/AffixEarthHealerMaster.prefab").OnSuccess(m => _healingCoreMasterPrefab = m);
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

            for (int i = CharacterBody.readOnlyInstancesList.Count - 1; i >= 0; i--)
            {
                CharacterBody body = CharacterBody.readOnlyInstancesList[i];

                if (_spawnOnBlacklist.Contains(body.bodyIndex))
                    continue;

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
}