using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfOptions.Options;
using RoR2;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("duplicate_all_characters", EffectWeightReductionPercentagePerActivation = 50f)]
    public sealed class DuplicateAllCharacters : BaseEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<bool> _allowDontDestroyOnLoadConfig;
        const bool ALLOW_DONT_DESTROY_ON_LOAD_DEFAULT_VALUE = false;

        static bool allowDontDestroyOnLoad => _allowDontDestroyOnLoadConfig?.Value ?? ALLOW_DONT_DESTROY_ON_LOAD_DEFAULT_VALUE;

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _allowDontDestroyOnLoadConfig = _effectInfo.BindConfig("Keep duplicated allies between stages", ALLOW_DONT_DESTROY_ON_LOAD_DEFAULT_VALUE, new ConfigDescription("Allows duplicated allies to come with you to the next stage.\nThis is disabled by default to prevent lag by repeatedly duplicating your drones."));

            addConfigOption(new CheckBoxOption(_allowDontDestroyOnLoadConfig));
        }

        public override void OnStart()
        {
            ReadOnlyCollection<CharacterBody> allCharacterBodies = CharacterBody.readOnlyInstancesList;
            for (int i = allCharacterBodies.Count - 1; i >= 0; i--)
            {
                CharacterBody body = allCharacterBodies[i];
                if (!body)
                    continue;

                if ((body.bodyFlags & CharacterBody.BodyFlags.Masterless) != 0)
                    continue;

                CharacterMaster master = body.master;
                if (!master)
                    continue;

                duplicateMaster(master);
            }
        }

        void duplicateMaster(CharacterMaster master)
        {
            CharacterBody body = master.GetBody();
            if (!body)
                return;

            MasterCopySpawnCard copySpawnCard = MasterCopySpawnCard.FromMaster(master, true, true);

            DirectorPlacementRule placement = new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Direct,
                position = body.footPosition
            };

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(copySpawnCard, placement, new Xoroshiro128Plus(RNG.nextUlong))
            {
                summonerBodyObject = body.gameObject,
                teamIndexOverride = master.teamIndex,
                ignoreTeamMemberLimit = true
            };

            spawnRequest.onSpawnedServer += result =>
            {
                if (!result.success || !result.spawnedInstance)
                    return;

                if (!allowDontDestroyOnLoad)
                {
                    if (result.spawnedInstance.GetComponent<SetDontDestroyOnLoad>())
                    {
                        GameObject.Destroy(result.spawnedInstance.GetComponent<SetDontDestroyOnLoad>());
                    }

                    if (Util.IsDontDestroyOnLoad(result.spawnedInstance))
                    {
                        SceneManager.MoveGameObjectToScene(result.spawnedInstance, SceneManager.GetActiveScene());
                    }
                }
            };

            DirectorCore.instance.TrySpawnObject(spawnRequest);
            UnityEngine.Object.Destroy(copySpawnCard);
        }
    }
}
