using RiskOfChaos.EffectHandling;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.AddressableAssets;
using BepInEx.Configuration;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect(EFFECT_ID, EffectRepetitionWeightExponent = 20f)]
    public class SpawnWarbanner : BaseEffect
    {
        const string EFFECT_ID = "SpawnWarbanner";

        static readonly GameObject _warbannerPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/WardOnLevel/WarbannerWard.prefab").WaitForCompletion();

#if DEBUG
        static SpawnWarbanner()
        {
            Log.Debug($".cctor called, {nameof(_warbannerPrefab)}={_warbannerPrefab}");
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
            string sectionName = ChaosEffectCatalog.GetConfigSectionName(EFFECT_ID);

            _warbannerItemCount = Main.Instance.Config.Bind<int>(new ConfigDefinition(sectionName, "Item Stack Count"), WARBANNER_ITEM_COUNT_DEFAULT_VALUE, new ConfigDescription("The amount of item stacks to mimic when spawning a warbanner"));
            ChaosEffectCatalog.AddEffectConfigOption(new IntSliderOption(_warbannerItemCount, new IntSliderConfig
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

        public override void OnStart()
        {
            int itemCount = warbannerItemCount;

            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (!body || (body.bodyFlags & CharacterBody.BodyFlags.Masterless) != 0)
                    continue;

                TeamComponent teamComponent = body.teamComponent;
                if (!teamComponent)
                    continue;

                GameObject warbannerObj = GameObject.Instantiate<GameObject>(_warbannerPrefab, body.footPosition, Quaternion.identity);
                warbannerObj.GetComponent<TeamFilter>().teamIndex = teamComponent.teamIndex;
                warbannerObj.GetComponent<BuffWard>().Networkradius = 8f + (8f * itemCount);
                NetworkServer.Spawn(warbannerObj);
            }
        }
    }
}
