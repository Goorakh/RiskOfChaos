using EntityStates;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using RoR2.Skills;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    /*
    [ChaosEffect("spawn_pulse_golem")]
    public sealed class SpawnPulseGolem : MonoBehaviour
    {
        static CharacterSpawnCard _cscPulseGolem;

        [ContentInitializer]
        static IEnumerator LoadContent(MasterPrefabAssetCollection masterPrefabs, BodyPrefabAssetCollection bodyPrefabs, SkillDefAssetCollection skillDefs, SkillFamilyAssetCollection skillFamilies)
        {
            AsyncOperationHandle<GameObject> golemMasterLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Golem/GolemMaster.prefab");
            AsyncOperationHandle<GameObject> golemBodyLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Golem/GolemBody.prefab");

            AsyncOperationHandle[] loadOperations = [golemMasterLoad, golemBodyLoad];
            yield return loadOperations.WaitForAllLoaded();

            if (golemMasterLoad.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error($"Failed to load {golemMasterLoad.LocationName}: {golemMasterLoad.OperationException}");
                yield break;
            }

            if (golemBodyLoad.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error($"Failed to load {golemBodyLoad.LocationName}: {golemBodyLoad.OperationException}");
                yield break;
            }

            SkillDef pulseGolemBodyLaser = ScriptableObject.CreateInstance<SkillDef>();
            ((UnityEngine.Object)pulseGolemBodyLaser).name = "PulseGolemBodyLaser";
            pulseGolemBodyLaser.skillName = "PulseLaser";
            pulseGolemBodyLaser.activationStateMachineName = "Weapon";
            pulseGolemBodyLaser.activationState = new SerializableEntityStateType(typeof(Content.EntityStates.PulseGolem.ChargeHook));
            pulseGolemBodyLaser.interruptPriority = InterruptPriority.Any;
            pulseGolemBodyLaser.baseRechargeInterval = 6f;
            pulseGolemBodyLaser.beginSkillCooldownOnSkillEnd = true;

            skillDefs.Add(pulseGolemBodyLaser);

            SkillFamily pulseGolemBodySkillFamily = ScriptableObject.CreateInstance<SkillFamily>();
            pulseGolemBodySkillFamily.variants = [new SkillFamily.Variant { skillDef = pulseGolemBodyLaser }];
            pulseGolemBodySkillFamily.defaultVariantIndex = 0;

            GameObject golemMasterPrefab = golemMasterLoad.Result;
            GameObject golemBodyPrefab = golemBodyLoad.Result;

            GameObject pulseGolemBodyPrefab = golemBodyPrefab.InstantiateNetworkedPrefab("PulseGolemBody");
            CharacterBody pulseGolemBody = pulseGolemBodyPrefab.GetComponent<CharacterBody>();
            pulseGolemBody.baseNameToken = "PULSE_GOLEM_BODY_NAME";
            pulseGolemBody.bodyFlags = CharacterBody.BodyFlags.ImmuneToExecutes;

            pulseGolemBody.baseMaxHealth = 50000f;
            pulseGolemBody.baseRegen = pulseGolemBody.baseMaxHealth * 0.01f;
            pulseGolemBody.baseArmor = 100;
            pulseGolemBody.baseMoveSpeed = 10f;
            pulseGolemBody.PerformAutoCalculateLevelStats();

            Destroy(pulseGolemBodyPrefab.GetComponent<DeathRewards>());

            SkillLocator pulseGolemSkillLocator = pulseGolemBody.GetComponent<SkillLocator>();
            Destroy(pulseGolemSkillLocator.secondary);
            pulseGolemSkillLocator.secondary = pulseGolemBodyPrefab.AddComponent<GenericSkill>();
            pulseGolemSkillLocator.secondary._skillFamily = pulseGolemBodySkillFamily;
            pulseGolemSkillLocator.secondary.skillName = "PulseLaser";

            bodyPrefabs.Add(pulseGolemBodyPrefab);

            GameObject pulseGolemMasterPrefab = golemMasterPrefab.InstantiateNetworkedPrefab("PulseGolemMaster");
            CharacterMaster pulseGolemMaster = pulseGolemMasterPrefab.GetComponent<CharacterMaster>();
            pulseGolemMaster.bodyPrefab = pulseGolemBodyPrefab;

            BaseAI pulseGolemAi = pulseGolemMaster.GetComponent<BaseAI>();
            pulseGolemAi.aimVectorMaxSpeed = 90f;
            pulseGolemAi.aimVectorDampTime = 0.1f;

            foreach (AISkillDriver skillDriver in pulseGolemMaster.GetComponents<AISkillDriver>())
            {
                if (skillDriver.skillSlot == SkillSlot.Secondary)
                {
                    skillDriver.maxDistance = 150f;
                }
            }

            masterPrefabs.Add(pulseGolemMasterPrefab);

            _cscPulseGolem = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            _cscPulseGolem.name = "cscPulseGolem";
            _cscPulseGolem.prefab = pulseGolemMasterPrefab;
            _cscPulseGolem.sendOverNetwork = true;
            _cscPulseGolem.hullSize = HullClassification.Golem;
            _cscPulseGolem.nodeGraphType = MapNodeGroup.GraphType.Ground;
            _cscPulseGolem.requiredFlags = NodeFlags.None;
            _cscPulseGolem.forbiddenFlags = NodeFlags.NoCharacterSpawn;
        }

        [SystemInitializer]
        static void Init()
        {
            if (_cscPulseGolem)
            {
                _cscPulseGolem.itemsToGrant = [
                    new ItemCountPair
                    {
                        itemDef = RoCContent.Items.PulseAway,
                        count = 1
                    }
                ];
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _cscPulseGolem.HasValidSpawnLocation();
        }

        ChaosEffectComponent _effectComponent;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

                DirectorCore.GetMonsterSpawnDistance(DirectorCore.MonsterSpawnDistance.Far, out float minDistance, out float maxDistance);

                DirectorPlacementRule placementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerApproximate(rng, minDistance, maxDistance);

                DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_cscPulseGolem, placementRule, rng)
                {
                    teamIndexOverride = TeamIndex.Monster,
                    ignoreTeamMemberLimit = true,
                };

                spawnRequest.SpawnWithFallbackPlacement(SpawnUtils.GetPlacementRule_AtRandomPlayerNearestNode(rng, minDistance, float.PositiveInfinity), SpawnUtils.GetBestValidRandomPlacementRule());
            }
        }
    }
    */
}
