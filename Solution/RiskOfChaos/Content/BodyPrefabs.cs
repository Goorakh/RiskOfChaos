using EntityStates;
using R2API;
using RiskOfChaos.Components;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.Content
{
    public static class BodyPrefabs
    {
        public static readonly GameObject ChaosFakeInteractorBodyPrefab;

        static BodyPrefabs()
        {
            {
                ChaosFakeInteractorBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/AltarSkeleton/AltarSkeletonBody.prefab").WaitForCompletion().InstantiateClone("ChaosFakeInteractorBody");

                Transform transform = ChaosFakeInteractorBodyPrefab.transform;
                transform.position = Vector3.zero;
                transform.rotation = Quaternion.identity;
                transform.localScale = Vector3.one;

                foreach (AkEvent akEvent in ChaosFakeInteractorBodyPrefab.GetComponents<AkEvent>())
                {
                    GameObject.Destroy(akEvent);
                }

                GameObject.Destroy(ChaosFakeInteractorBodyPrefab.GetComponent<CharacterDeathBehavior>());
                GameObject.Destroy(ChaosFakeInteractorBodyPrefab.GetComponent<GameObjectUnlockableFilter>());

                ModelLocator modelLocator = ChaosFakeInteractorBodyPrefab.GetComponent<ModelLocator>();
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

                CharacterBody body = ChaosFakeInteractorBodyPrefab.GetComponent<CharacterBody>();

                body.baseNameToken = "CHAOS_FAKE_INTERACTOR_BODY_NAME";
                body.baseMaxHealth = 1e9F; // 10^9
                body.baseRegen = 1e9F; // 10^9

                EntityStateMachine entityStateMachine = ChaosFakeInteractorBodyPrefab.GetComponent<EntityStateMachine>();
                entityStateMachine.initialStateType = new SerializableEntityStateType(typeof(Idle));
                entityStateMachine.mainStateType = new SerializableEntityStateType(typeof(Idle));

                TeamComponent teamComponent = ChaosFakeInteractorBodyPrefab.GetComponent<TeamComponent>();
                teamComponent._teamIndex = TeamIndex.None;

                ChaosFakeInteractorBodyPrefab.AddComponent<Interactor>();
                ChaosFakeInteractorBodyPrefab.AddComponent<SetDontDestroyOnLoad>();
                ChaosFakeInteractorBodyPrefab.AddComponent<DestroyOnRunEnd>();

                ChaosFakeInteractorBodyPrefab.AddComponent<ChaosInteractor>();
                ChaosFakeInteractorBodyPrefab.AddComponent<ExcludeFromBodyInstancesList>();

                static void trySpawnChaosInteractor()
                {
                    if (!ChaosInteractor.Instance)
                    {
                        NetworkServer.Spawn(GameObject.Instantiate(ChaosFakeInteractorBodyPrefab));
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

        internal static void AddBodyPrefabsTo(NamedAssetCollection<GameObject> bodyPrefabs)
        {
            bodyPrefabs.Add([
                ChaosFakeInteractorBodyPrefab
            ]);
        }
    }
}
