using HG.Coroutines;
using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.CharacterAI;
using RoR2.ContentManagement;
using RoR2.Navigation;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_invincible_lemurian")]
    public sealed class SpawnIncincibleLemurian : NetworkBehaviour
    {
        static CharacterSpawnCard _cscInvincibleLemurian;
        static CharacterSpawnCard _cscInvincibleLemurianBruiser;

        [ContentInitializer]
        static IEnumerator LoadContent(ContentIntializerArgs args)
        {
            ParallelProgressCoroutine parallelCoroutine = new ParallelProgressCoroutine(args.ProgressReceiver);

            AsyncOperationHandle<GameObject> lemurianBodyPrefabLoad = AddressableUtil.LoadTempAssetAsync<GameObject>(AddressableGuids.RoR2_Base_Lemurian_LemurianBody_prefab);
            parallelCoroutine.Add(lemurianBodyPrefabLoad);

            AsyncOperationHandle<GameObject> lemurianMasterPrefabLoad = AddressableUtil.LoadTempAssetAsync<GameObject>(AddressableGuids.RoR2_Base_Lemurian_LemurianMaster_prefab);
            parallelCoroutine.Add(lemurianMasterPrefabLoad);

            AsyncOperationHandle<GameObject> lemurianBruiserBodyPrefabLoad = AddressableUtil.LoadTempAssetAsync<GameObject>(AddressableGuids.RoR2_Base_LemurianBruiser_LemurianBruiserBody_prefab);
            parallelCoroutine.Add(lemurianBruiserBodyPrefabLoad);

            AsyncOperationHandle<GameObject> lemurianBruiserMasterPrefabLoad = AddressableUtil.LoadTempAssetAsync<GameObject>(AddressableGuids.RoR2_Base_LemurianBruiser_LemurianBruiserMaster_prefab);
            parallelCoroutine.Add(lemurianBruiserMasterPrefabLoad);

            yield return parallelCoroutine;

            const CharacterBody.BodyFlags INVINCIBLE_LEMURIAN_BODY_FLAGS = CharacterBody.BodyFlags.IgnoreFallDamage | CharacterBody.BodyFlags.ImmuneToExecutes | CharacterBody.BodyFlags.ImmuneToVoidDeath | CharacterBody.BodyFlags.ImmuneToLava;

            const float MOVE_SPEED_MULT = 1f / 2f;
            const float ATTACK_SPEED_MULT = 1f / 1.5f;

            GameObject invincibleLemurianBody;
            GameObject invincibleLemurianMaster;
            {
                invincibleLemurianBody = lemurianBodyPrefabLoad.Result.InstantiateNetworkedPrefab("InvincibleLemurianBody");
                CharacterBody bodyPrefab = invincibleLemurianBody.GetComponent<CharacterBody>();
                bodyPrefab.baseNameToken = "INVINCIBLE_LEMURIAN_BODY_NAME";
                bodyPrefab.bodyFlags = INVINCIBLE_LEMURIAN_BODY_FLAGS;
                bodyPrefab.baseMoveSpeed *= MOVE_SPEED_MULT;
                bodyPrefab.baseAttackSpeed *= ATTACK_SPEED_MULT;
                bodyPrefab.bodyFlags |= CharacterBody.BodyFlags.Ungrabbable;

                Destroy(invincibleLemurianBody.GetComponent<DeathRewards>());

                InvincibleLemurianController invincibleLemurianController = invincibleLemurianBody.AddComponent<InvincibleLemurianController>();

                invincibleLemurianMaster = lemurianMasterPrefabLoad.Result.InstantiateNetworkedPrefab("InvincibleLemurianMaster");
                CharacterMaster masterPrefab = invincibleLemurianMaster.GetComponent<CharacterMaster>();
                masterPrefab.bodyPrefab = invincibleLemurianBody;

                foreach (BaseAI baseAI in masterPrefab.GetComponents<BaseAI>())
                {
                    if (baseAI)
                    {
                        baseAI.fullVision = true;
                        baseAI.neverRetaliateFriendlies = true;
                        baseAI.fullVision = true;
                        baseAI.xrayVision = true;
                    }
                }

                _cscInvincibleLemurian = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                _cscInvincibleLemurian.name = "cscInvincibleLemurian";
                _cscInvincibleLemurian.prefab = invincibleLemurianMaster;
                _cscInvincibleLemurian.sendOverNetwork = true;
                _cscInvincibleLemurian.hullSize = bodyPrefab.hullClassification;
                _cscInvincibleLemurian.nodeGraphType = MapNodeGroup.GraphType.Ground;
                _cscInvincibleLemurian.requiredFlags = NodeFlags.None;
                _cscInvincibleLemurian.forbiddenFlags = NodeFlags.NoCharacterSpawn;
            }

            GameObject invincibleLemurianBruiserBody;
            GameObject invincibleLemurianBruiserMaster;
            {
                invincibleLemurianBruiserBody = lemurianBruiserBodyPrefabLoad.Result.InstantiateNetworkedPrefab("InvincibleLemurianBruiserBody");
                CharacterBody bodyPrefab = invincibleLemurianBruiserBody.GetComponent<CharacterBody>();
                bodyPrefab.baseNameToken = "INVINCIBLE_LEMURIAN_ELDER_BODY_NAME";
                bodyPrefab.bodyFlags = INVINCIBLE_LEMURIAN_BODY_FLAGS;
                bodyPrefab.baseMoveSpeed *= MOVE_SPEED_MULT;
                bodyPrefab.baseAttackSpeed *= ATTACK_SPEED_MULT;
                bodyPrefab.bodyFlags |= CharacterBody.BodyFlags.Ungrabbable;

                Destroy(invincibleLemurianBruiserBody.GetComponent<DeathRewards>());

                InvincibleLemurianController invincibleLemurianController = invincibleLemurianBruiserBody.AddComponent<InvincibleLemurianController>();

                invincibleLemurianBruiserMaster = lemurianBruiserMasterPrefabLoad.Result.InstantiateNetworkedPrefab("InvincibleLemurianBruiserMaster");
                CharacterMaster masterPrefab = invincibleLemurianBruiserMaster.GetComponent<CharacterMaster>();
                masterPrefab.bodyPrefab = invincibleLemurianBruiserBody;

                foreach (BaseAI baseAI in masterPrefab.GetComponents<BaseAI>())
                {
                    if (baseAI)
                    {
                        baseAI.fullVision = true;
                        baseAI.neverRetaliateFriendlies = true;
                        baseAI.fullVision = true;
                        baseAI.xrayVision = true;
                    }
                }

                _cscInvincibleLemurianBruiser = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                _cscInvincibleLemurianBruiser.name = "cscInvincibleLemurianBruiser";
                _cscInvincibleLemurianBruiser.prefab = invincibleLemurianBruiserMaster;
                _cscInvincibleLemurianBruiser.sendOverNetwork = true;
                _cscInvincibleLemurianBruiser.hullSize = bodyPrefab.hullClassification;
                _cscInvincibleLemurianBruiser.nodeGraphType = MapNodeGroup.GraphType.Ground;
                _cscInvincibleLemurianBruiser.requiredFlags = NodeFlags.None;
                _cscInvincibleLemurianBruiser.forbiddenFlags = NodeFlags.NoCharacterSpawn;
            }

            args.ContentPack.bodyPrefabs.Add([
                invincibleLemurianBody,
                invincibleLemurianBruiserBody,
            ]);

            args.ContentPack.masterPrefabs.Add([
                invincibleLemurianMaster,
                invincibleLemurianBruiserMaster,
            ]);
        }

        static readonly SpawnPool<CharacterSpawnCard> _spawnPool = new SpawnPool<CharacterSpawnCard>();

        [SystemInitializer]
        static void Init()
        {
            _spawnPool.EnsureCapacity(2);

            _spawnPool.AddEntry(_cscInvincibleLemurian, new SpawnPoolEntryParameters(0.95f));
            _spawnPool.AddEntry(_cscInvincibleLemurianBruiser, new SpawnPoolEntryParameters(0.05f));
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _spawnPool.AnyAvailable;
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        AssetOrDirectReference<CharacterSpawnCard> _lemurianSpawnCardRef;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void OnDestroy()
        {
            _lemurianSpawnCardRef?.Reset();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _lemurianSpawnCardRef = _spawnPool.PickRandomEntry(_rng);
            _lemurianSpawnCardRef.CallOnLoaded(onSpawnCardLoadedServer);
        }

        [Server]
        void onSpawnCardLoadedServer(CharacterSpawnCard spawnCard)
        {
            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, SpawnUtils.GetBestValidRandomPlacementRule(), _rng)
            {
                teamIndexOverride = TeamIndex.Monster,
                ignoreTeamMemberLimit = true
            };

            DirectorCore.instance.TrySpawnObject(spawnRequest);
        }
    }
}
