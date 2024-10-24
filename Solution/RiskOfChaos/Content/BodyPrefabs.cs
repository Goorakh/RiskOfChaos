using RiskOfChaos.Components;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Patches;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Content
{
    partial class RoCContent
    {
        partial class BodyPrefabs
        {
            [ContentInitializer]
            static void LoadContent(BodyPrefabAssetCollection bodyPrefabs)
            {
                // ChaosFakeInteractorBody
                {
                    GameObject bodyPrefab = Prefabs.CreateNetworkedPrefab(nameof(ChaosFakeInteractorBody), [
                        typeof(SetDontDestroyOnLoad),
                        typeof(DestroyOnRunEnd),
                        typeof(CharacterBody),
                        typeof(HealthComponent),
                        typeof(ModelLocator),
                        typeof(Interactor),
                        typeof(ChaosInteractor)
                    ]);

                    CharacterBody body = bodyPrefab.GetComponent<CharacterBody>();
                    body.baseNameToken = "CHAOS_FAKE_INTERACTOR_BODY_NAME";
                    body.baseMaxHealth = 1e9F; // 10^9
                    body.baseRegen = 1e9F; // 10^9

                    Transform modelBase = new GameObject("ModelBase").transform;
                    modelBase.SetParent(bodyPrefab.transform);
                    modelBase.localPosition = Vector3.zero;
                    modelBase.localRotation = Quaternion.identity;

                    ModelLocator modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                    modelLocator._modelTransform = modelBase;
                    modelLocator.autoUpdateModelTransform = false;
                    modelLocator.dontDetatchFromParent = true;

                    TeamComponent teamComponent = bodyPrefab.GetComponent<TeamComponent>();
                    teamComponent._teamIndex = TeamIndex.None;

                    bodyPrefabs.Add(bodyPrefab);

                    HiddenCharacterBodiesPatch.HideBody(bodyPrefab);
                }
            }

            [SystemInitializer]
            static void InitHooks()
            {
                static void ensureChaosInteractorSpawned()
                {
                    if (!ChaosInteractor.Instance)
                    {
                        NetworkServer.Spawn(GameObject.Instantiate(ChaosFakeInteractorBody));
                    }
                }

                Run.onRunStartGlobal += _ =>
                {
                    if (!NetworkServer.active)
                        return;

                    RoR2Application.onFixedUpdate += ensureChaosInteractorSpawned;
                };

                Run.onRunDestroyGlobal += _ =>
                {
                    RoR2Application.onFixedUpdate -= ensureChaosInteractorSpawned;
                };
            }
        }
    }
}
