using RiskOfChaos.Components;
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
        static IEnumerator LoadContent(MasterPrefabAssetCollection masterPrefabs, BodyPrefabAssetCollection bodyPrefabs)
        {
            List<AsyncOperationHandle> asyncOperations = new List<AsyncOperationHandle>(4);

            AsyncOperationHandle<GameObject> lemurianBodyPrefabLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lemurian/LemurianBody.prefab");
            asyncOperations.Add(lemurianBodyPrefabLoad);

            AsyncOperationHandle<GameObject> lemurianMasterPrefabLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lemurian/LemurianMaster.prefab");
            asyncOperations.Add(lemurianMasterPrefabLoad);

            AsyncOperationHandle<GameObject> lemurianBruiserBodyPrefabLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LemurianBruiser/LemurianBruiserBody.prefab");
            asyncOperations.Add(lemurianBruiserBodyPrefabLoad);

            AsyncOperationHandle<GameObject> lemurianBruiserMasterPrefabLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LemurianBruiser/LemurianBruiserMaster.prefab");
            asyncOperations.Add(lemurianBruiserMasterPrefabLoad);

            yield return asyncOperations.WaitForAllLoaded();

            const CharacterBody.BodyFlags INVINCIBLE_LEMURIAN_BODY_FLAGS = CharacterBody.BodyFlags.IgnoreFallDamage | CharacterBody.BodyFlags.ImmuneToExecutes | CharacterBody.BodyFlags.ImmuneToVoidDeath | CharacterBody.BodyFlags.ImmuneToLava;

            // InvincibleLemurian
            {
                GameObject bodyPrefabObj = lemurianBodyPrefabLoad.Result.InstantiateNetworkedPrefab("InvincibleLemurianBody");
                CharacterBody bodyPrefab = bodyPrefabObj.GetComponent<CharacterBody>();
                bodyPrefab.baseNameToken = "INVINCIBLE_LEMURIAN_BODY_NAME";
                bodyPrefab.bodyFlags = INVINCIBLE_LEMURIAN_BODY_FLAGS;

                Destroy(bodyPrefabObj.GetComponent<DeathRewards>());

                InvincibleLemurianController invincibleLemurianController = bodyPrefabObj.AddComponent<InvincibleLemurianController>();

                bodyPrefabs.Add(bodyPrefabObj);

                GameObject masterPrefabObj = lemurianMasterPrefabLoad.Result.InstantiateNetworkedPrefab("InvincibleLemurianMaster");
                CharacterMaster masterPrefab = masterPrefabObj.GetComponent<CharacterMaster>();
                masterPrefab.bodyPrefab = bodyPrefabObj;

                foreach (BaseAI baseAI in masterPrefab.GetComponents<BaseAI>())
                {
                    if (baseAI)
                    {
                        baseAI.fullVision = true;
                        baseAI.neverRetaliateFriendlies = true;
                    }
                }

                masterPrefabs.Add(masterPrefabObj);

                _cscInvincibleLemurian = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                _cscInvincibleLemurian.name = "cscInvincibleLemurian";
                _cscInvincibleLemurian.prefab = masterPrefabObj;
                _cscInvincibleLemurian.sendOverNetwork = true;
                _cscInvincibleLemurian.hullSize = bodyPrefab.hullClassification;
                _cscInvincibleLemurian.nodeGraphType = MapNodeGroup.GraphType.Ground;
                _cscInvincibleLemurian.requiredFlags = NodeFlags.None;
                _cscInvincibleLemurian.forbiddenFlags = NodeFlags.NoCharacterSpawn;
            }

            // InvincibleLemurianBruiser
            {
                GameObject bodyPrefabObj = lemurianBruiserBodyPrefabLoad.Result.InstantiateNetworkedPrefab("InvincibleLemurianBruiserBody");
                CharacterBody bodyPrefab = bodyPrefabObj.GetComponent<CharacterBody>();
                bodyPrefab.baseNameToken = "INVINCIBLE_LEMURIAN_ELDER_BODY_NAME";
                bodyPrefab.bodyFlags = INVINCIBLE_LEMURIAN_BODY_FLAGS;

                Destroy(bodyPrefabObj.GetComponent<DeathRewards>());

                InvincibleLemurianController invincibleLemurianController = bodyPrefabObj.AddComponent<InvincibleLemurianController>();

                bodyPrefabs.Add(bodyPrefabObj);

                GameObject masterPrefabObj = lemurianBruiserMasterPrefabLoad.Result.InstantiateNetworkedPrefab("InvincibleLemurianBruiserMaster");
                CharacterMaster masterPrefab = masterPrefabObj.GetComponent<CharacterMaster>();
                masterPrefab.bodyPrefab = bodyPrefabObj;

                foreach (BaseAI baseAI in masterPrefab.GetComponents<BaseAI>())
                {
                    if (baseAI)
                    {
                        baseAI.fullVision = true;
                        baseAI.neverRetaliateFriendlies = true;
                    }
                }

                masterPrefabs.Add(masterPrefabObj);

                _cscInvincibleLemurianBruiser = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                _cscInvincibleLemurianBruiser.name = "cscInvincibleLemurianBruiser";
                _cscInvincibleLemurianBruiser.prefab = masterPrefabObj;
                _cscInvincibleLemurianBruiser.sendOverNetwork = true;
                _cscInvincibleLemurianBruiser.hullSize = bodyPrefab.hullClassification;
                _cscInvincibleLemurianBruiser.nodeGraphType = MapNodeGroup.GraphType.Ground;
                _cscInvincibleLemurianBruiser.requiredFlags = NodeFlags.None;
                _cscInvincibleLemurianBruiser.forbiddenFlags = NodeFlags.NoCharacterSpawn;
            }
        }

        static readonly SpawnPool<CharacterSpawnCard> _spawnPool = new SpawnPool<CharacterSpawnCard>
        {
            RequiredExpansionsProvider = SpawnPoolUtils.CharacterSpawnCardExpansionsProvider
        };

        [SystemInitializer]
        static void Init()
        {
            _spawnPool.EnsureCapacity(2);

            _spawnPool.AddEntry(_cscInvincibleLemurian, 0.95f);
            _spawnPool.AddEntry(_cscInvincibleLemurianBruiser, 0.05f);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _spawnPool.AnyAvailable;
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        CharacterSpawnCard _selectedSpawnCard;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _selectedSpawnCard = _spawnPool.PickRandomEntry(_rng);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_selectedSpawnCard, SpawnUtils.GetBestValidRandomPlacementRule(), _rng)
            {
                teamIndexOverride = TeamIndex.Monster,
                ignoreTeamMemberLimit = true
            };

            DirectorCore.instance.TrySpawnObject(spawnRequest);
        }
    }
}
