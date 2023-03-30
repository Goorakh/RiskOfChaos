using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_warbanner", EffectWeightReductionPercentagePerActivation = 20f)]
    public sealed class SpawnWarbanner : CoroutineEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static readonly GameObject _warbannerPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/WardOnLevel/WarbannerWard.prefab").WaitForCompletion();

#if DEBUG
        static SpawnWarbanner()
        {
            Log.Debug($"{nameof(_warbannerPrefab)}={_warbannerPrefab}");
        }
#endif

        static ConfigEntry<int> _warbannerItemCount;
        const int WARBANNER_ITEM_COUNT_DEFAULT_VALUE = 2;

        static int warbannerItemCount
        {
            get
            {
                return _warbannerItemCount != null ? Mathf.Max(_warbannerItemCount.Value, 1) : WARBANNER_ITEM_COUNT_DEFAULT_VALUE;
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _warbannerItemCount = Main.Instance.Config.Bind<int>(new ConfigDefinition(_effectInfo.ConfigSectionName, "Item Stack Count"), WARBANNER_ITEM_COUNT_DEFAULT_VALUE, new ConfigDescription("The amount of item stacks to mimic when spawning a warbanner"));
            addConfigOption(new IntSliderOption(_warbannerItemCount, new IntSliderConfig
            {
                min = 1,
                max = 10
            }));
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _warbannerPrefab;
        }

        readonly struct WarbannerSpawnData
        {
            public readonly CharacterBody Owner;
            public readonly float Radius;

            public WarbannerSpawnData(CharacterBody owner, float radius)
            {
                Owner = owner;
                Radius = radius;
            }

            public readonly void Spawn()
            {
                if (!Owner)
                    return;

                TeamComponent teamComponent = Owner.teamComponent;
                if (!teamComponent)
                    return;

                GameObject warbannerObj = GameObject.Instantiate<GameObject>(_warbannerPrefab, Owner.footPosition, Quaternion.identity);
                warbannerObj.GetComponent<TeamFilter>().teamIndex = teamComponent.teamIndex;
                warbannerObj.GetComponent<BuffWard>().Networkradius = Radius;
                NetworkServer.Spawn(warbannerObj);
            }
        }

        protected override IEnumerator onStart()
        {
            float radius = 8f + (8f * warbannerItemCount);

            List<WarbannerSpawnData> warbannerSpawnQueue = new List<WarbannerSpawnData>();
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (!body || (body.bodyFlags & CharacterBody.BodyFlags.Masterless) != 0)
                    continue;

                TeamComponent teamComponent = body.teamComponent;
                if (!teamComponent)
                    continue;

                warbannerSpawnQueue.Add(new WarbannerSpawnData(body, radius));
            }

            const int MAX_SPAWNS_PER_FRAME = 2;
            for (int i = 0; i < warbannerSpawnQueue.Count; i++)
            {
                warbannerSpawnQueue[i].Spawn();

                if ((i + 1) % MAX_SPAWNS_PER_FRAME == 0)
                {
                    yield return new WaitForSecondsRealtime(1f / 15f);
                }
            }
        }
    }
}
