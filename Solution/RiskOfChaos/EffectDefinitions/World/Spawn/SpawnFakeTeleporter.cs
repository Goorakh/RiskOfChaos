using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_fake_teleporter", DefaultSelectionWeight = 0.8f)]
    public class SpawnFakeTeleporter : NetworkBehaviour
    {
        static InteractableSpawnCard _fakeTeleporterSpawnCard;

        [ContentInitializer]
        static IEnumerator LoadContent(NetworkedPrefabAssetCollection networkedPrefabs)
        {
            List<AsyncOperationHandle> asyncOperations = [];

            AsyncOperationHandle<InteractableSpawnCard> teleporterSpawnCardLoad = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/Teleporters/iscTeleporter.asset");
            asyncOperations.Add(teleporterSpawnCardLoad);
            teleporterSpawnCardLoad.OnSuccess(teleporterSpawnCard =>
            {
                GameObject teleporterPrefab = teleporterSpawnCard.prefab;
                Transform teleporterModel = teleporterPrefab.GetComponent<ModelLocator>().modelTransform;

                GameObject fakeTeleporterPrefab = Prefabs.CreateNetworkedPrefab("FakeTeleporter", [
                    typeof(FakeTeleporterInteraction),
                    typeof(Highlight),
                    typeof(GenericDisplayNameProvider),
                    typeof(ModelLocator),
                    typeof(EntityStateMachine),
                    typeof(NetworkStateMachine),
                    typeof(CombatDirector),
                    typeof(PerpetualBossController),
                    typeof(PingInfoProvider)
                ]);

                Transform fakeTeleporterModel = Instantiate(teleporterModel, fakeTeleporterPrefab.transform);
                fakeTeleporterModel.name = teleporterModel.name;
                fakeTeleporterModel.localPosition = teleporterModel.localPosition;
                fakeTeleporterModel.localRotation = teleporterModel.localRotation;
                fakeTeleporterModel.localScale = teleporterModel.localScale;

                static GameObject mapObjectReference(GameObject referenceObject, GameObject referenceRoot, GameObject mappedRoot)
                {
                    if (!referenceObject)
                        return null;
                    
                    if (!referenceObject.transform.IsChildOf(referenceRoot.transform))
                    {
                        Log.Warning($"{referenceObject} is not a child of {referenceRoot}, cannot convert reference");
                        return referenceObject;
                    }

                    string transformPath = Util.BuildPrefabTransformPath(referenceRoot.transform, referenceObject.transform);
                    Transform mappedComponentTransform = mappedRoot.transform.Find(transformPath);
                    if (!mappedComponentTransform)
                    {
                        Log.Error($"No object with path '{transformPath}' found in {mappedRoot}");
                        return null;
                    }

                    return mappedComponentTransform.gameObject;
                }

                static T mapComponentReference<T>(T referenceComponent, GameObject referenceRoot, GameObject mappedRoot) where T : Component
                {
                    if (!referenceComponent)
                        return null;

                    GameObject mappedObject = mapObjectReference(referenceComponent.gameObject, referenceRoot, mappedRoot);
                    if (!mappedObject)
                        return null;

                    Type componentType = referenceComponent.GetType();
                    T mappedComponent = mappedObject.GetComponent(componentType) as T;
                    if (!mappedComponent)
                    {
                        Log.Error($"Mapped object {mappedObject} (root={mappedRoot}) does not have component {typeof(T).FullName}");
                        return null;
                    }

                    return mappedComponent;
                }

                static void autoCopyComponentValues<T>(T src, GameObject srcRoot, T dest, GameObject destRoot) where T : MonoBehaviour
                {
                    if (!src)
                        throw new ArgumentNullException(nameof(src));

                    if (!dest)
                        throw new ArgumentNullException(nameof(dest));

                    dest.enabled = src.enabled;

                    foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if ((field.IsPublic || field.GetCustomAttribute<SerializeField>() != null) && field.GetCustomAttribute<NonSerializedAttribute>() == null)
                        {
                            object value = field.GetValue(src);

                            if (value is Component componentValue)
                            {
                                value = mapComponentReference(componentValue, srcRoot, destRoot);
                            }

                            field.SetValue(dest, value);
                        }
                    }
                }

                static void autoCopyComponent<T>(GameObject srcObject, GameObject destObject) where T : MonoBehaviour
                {
                    T srcComponent = srcObject.GetComponent<T>();
                    if (!srcComponent)
                    {
                        Log.Warning($"{srcObject} is missing {typeof(T).Name} component");
                        return;
                    }

                    T destComponent = destObject.GetComponent<T>();
                    if (!destComponent)
                    {
                        Log.Warning($"{destObject} is missing {typeof(T).Name} component");
                        return;
                    }

                    autoCopyComponentValues(srcComponent, srcObject, destComponent, destObject);
                }

                autoCopyComponent<Highlight>(teleporterPrefab, fakeTeleporterPrefab);
                autoCopyComponent<GenericDisplayNameProvider>(teleporterPrefab, fakeTeleporterPrefab);
                autoCopyComponent<ModelLocator>(teleporterPrefab, fakeTeleporterPrefab);

                foreach (EntityLocator fakeTeleporterEntityLocator in fakeTeleporterPrefab.GetComponentsInChildren<EntityLocator>(true))
                {
                    fakeTeleporterEntityLocator.entity = mapObjectReference(fakeTeleporterEntityLocator.entity, teleporterPrefab, fakeTeleporterPrefab);
                }

                TeleporterInteraction teleporterInteraction = teleporterPrefab.GetComponent<TeleporterInteraction>();

                List<string> portalIndicatorChildNames = ["ShopPortalIndicator", "GoldshoresPortalIndicator", "MSPortalIndicator"];
                PortalSpawner[] teleporterPortalSpawners = teleporterInteraction.GetComponents<PortalSpawner>();
                if (teleporterPortalSpawners.Length > 0)
                {
                    portalIndicatorChildNames.EnsureCapacity(portalIndicatorChildNames.Count + teleporterPortalSpawners.Length);
                    foreach (PortalSpawner portalSpawner in teleporterPortalSpawners)
                    {
                        if (!string.IsNullOrEmpty(portalSpawner.previewChildName) && !portalIndicatorChildNames.Contains(portalSpawner.previewChildName))
                        {
                            portalIndicatorChildNames.Add(portalSpawner.previewChildName);
                        }
                    }
                }

                EntityStateMachine fakeTeleporterStateMachine = fakeTeleporterPrefab.GetComponent<EntityStateMachine>();
                fakeTeleporterStateMachine.initialStateType = FakeTeleporterInteraction.IdleStateType;
                fakeTeleporterStateMachine.mainStateType = FakeTeleporterInteraction.IdleStateType;

                NetworkStateMachine fakeTeleporterNetworkStateMachine = fakeTeleporterPrefab.GetComponent<NetworkStateMachine>();
                fakeTeleporterNetworkStateMachine.stateMachines = [fakeTeleporterStateMachine];

                CombatDirector bossDirector = fakeTeleporterPrefab.GetComponent<CombatDirector>();
                bossDirector.enabled = false;
                bossDirector.expRewardCoefficient = 0f;
                bossDirector.goldRewardCoefficient = 0f;
                bossDirector.targetPlayers = false;
                bossDirector.ignoreTeamSizeLimit = true;
                bossDirector.currentSpawnTarget = fakeTeleporterPrefab.gameObject;
                bossDirector.eliteBias = 1f;

                PerpetualBossController perpetualBossController = fakeTeleporterPrefab.GetComponent<PerpetualBossController>();
                perpetualBossController.enabled = false;
                perpetualBossController.BossDirector = bossDirector;
                perpetualBossController.CreditMultiplier = 4.5f;

                FakeTeleporterInteraction fakeTeleporterInteraction = fakeTeleporterPrefab.GetComponent<FakeTeleporterInteraction>();
                fakeTeleporterInteraction.BossController = perpetualBossController;
                fakeTeleporterInteraction.MainStateMachine = fakeTeleporterStateMachine;
                fakeTeleporterInteraction.BeginContextString = teleporterInteraction.beginContextString;
                fakeTeleporterInteraction.DiscoveryRadius = teleporterInteraction.discoveryRadius;
                fakeTeleporterInteraction.SyncTeleporterChildActivations = ["BossShrineSymbol", "TimeCrystalProps", "TimeCrystalBeaconBlocker", .. portalIndicatorChildNames];

                PingInfoProvider fakeTpPingInfoProvider = fakeTeleporterPrefab.GetComponent<PingInfoProvider>();
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texTeleporterIconOutlined.png").OnSuccess(tpIcon =>
                {
                    fakeTpPingInfoProvider.pingIconOverride = tpIcon;
                });

                networkedPrefabs.Add(fakeTeleporterPrefab);

                _fakeTeleporterSpawnCard = Instantiate(teleporterSpawnCard);
                _fakeTeleporterSpawnCard.name = "iscFakeTeleporter";
                _fakeTeleporterSpawnCard.prefab = fakeTeleporterPrefab;
            });

            yield return asyncOperations.WaitForAllLoaded();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _fakeTeleporterSpawnCard && _fakeTeleporterSpawnCard.HasValidSpawnLocation();
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                DirectorPlacementRule placementRule = SpawnUtils.GetBestValidRandomPlacementRule();
                DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_fakeTeleporterSpawnCard, placementRule, _rng);

                DirectorCore.instance.TrySpawnObject(spawnRequest);
            }
        }
    }
}
