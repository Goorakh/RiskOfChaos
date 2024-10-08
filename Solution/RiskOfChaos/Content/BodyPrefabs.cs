using EntityStates;
using RiskOfChaos.Components;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Content
{
    static class BodyPrefabs
    {
        [ContentInitializer]
        static IEnumerator LoadContent(BodyPrefabAssetCollection bodyPrefabs)
        {
            List<AsyncOperationHandle> asyncOperations = [];

            // ChaosFakeInteractorBody
            {
                AsyncOperationHandle<GameObject> altarSkeletonLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/AltarSkeleton/AltarSkeletonBody.prefab");
                altarSkeletonLoad.Completed += handle =>
                {
                    GameObject bodyPrefab = handle.Result.InstantiateNetworkedPrefab(nameof(RoCContent.BodyPrefabs.ChaosFakeInteractorBody), 0x776559F5);

                    Transform transform = bodyPrefab.transform;
                    transform.position = Vector3.zero;
                    transform.rotation = Quaternion.identity;
                    transform.localScale = Vector3.one;

                    foreach (AkEvent akEvent in bodyPrefab.GetComponents<AkEvent>())
                    {
                        GameObject.Destroy(akEvent);
                    }

                    GameObject.Destroy(bodyPrefab.GetComponent<CharacterDeathBehavior>());
                    GameObject.Destroy(bodyPrefab.GetComponent<GameObjectUnlockableFilter>());

                    ModelLocator modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                    modelLocator.dontDetatchFromParent = true;

                    Transform modelBase = transform.Find("ModelBase");
                    if (modelBase)
                    {
                        for (int i = 0; i < modelBase.childCount; i++)
                        {
                            GameObject.Destroy(modelBase.GetChild(i).gameObject);
                        }

                        GameObject model = new GameObject("EmptyModel");
                        model.transform.SetParent(modelBase, false);

                        modelLocator._modelTransform = model.transform;
                    }

                    CharacterBody body = bodyPrefab.GetComponent<CharacterBody>();

                    body.baseNameToken = "CHAOS_FAKE_INTERACTOR_BODY_NAME";
                    body.baseMaxHealth = 1e9F; // 10^9
                    body.baseRegen = 1e9F; // 10^9

                    EntityStateMachine entityStateMachine = bodyPrefab.GetComponent<EntityStateMachine>();
                    entityStateMachine.initialStateType = new SerializableEntityStateType(typeof(Idle));
                    entityStateMachine.mainStateType = new SerializableEntityStateType(typeof(Idle));

                    TeamComponent teamComponent = bodyPrefab.GetComponent<TeamComponent>();
                    teamComponent._teamIndex = TeamIndex.None;

                    bodyPrefab.AddComponent<Interactor>();
                    bodyPrefab.AddComponent<SetDontDestroyOnLoad>();
                    bodyPrefab.AddComponent<DestroyOnRunEnd>();

                    bodyPrefab.AddComponent<ChaosInteractor>();
                    bodyPrefab.AddComponent<ExcludeFromBodyInstancesList>();

                    bodyPrefabs.Add(bodyPrefab);
                };

                asyncOperations.Add(altarSkeletonLoad);
            }

            yield return asyncOperations.WaitForAllLoaded();
        }

        [SystemInitializer]
        static void InitHooks()
        {
            static void trySpawnChaosInteractor()
            {
                if (!ChaosInteractor.Instance)
                {
                    NetworkServer.Spawn(GameObject.Instantiate(RoCContent.BodyPrefabs.ChaosFakeInteractorBody));
                }
            }

            Run.onRunStartGlobal += _ =>
            {
                if (!NetworkServer.active)
                    return;

                RoR2Application.onFixedUpdate += trySpawnChaosInteractor;
            };

            Run.onRunDestroyGlobal += _ =>
            {
                RoR2Application.onFixedUpdate -= trySpawnChaosInteractor;
            };
        }
    }
}
