using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using RoR2.Navigation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_survivors")]
    public sealed class SpawnSurvivors : NetworkBehaviour
    {
        static readonly SpawnPool<SurvivorDef> _spawnPool = new SpawnPool<SurvivorDef>();

        [EffectConfig]
        static readonly ConfigHolder<int> _numPodSpawns =
            ConfigFactory<int>.CreateConfig("Pod Spawn Count", 5)
                              .Description("The amount of pods to spawn")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        [SystemInitializer(typeof(SurvivorCatalog), typeof(MasterCatalog), typeof(UnlockableCatalog), typeof(ExpansionUtils))]
        static void Init()
        {
            _spawnPool.EnsureCapacity(SurvivorCatalog.survivorCount);

            foreach (SurvivorDef survivorDef in SurvivorCatalog.allSurvivorDefs)
            {
                MasterCatalog.MasterIndex aiMasterIndex = MasterCatalog.FindAiMasterIndexForBody(BodyCatalog.FindBodyIndex(survivorDef.bodyPrefab));
                if (!aiMasterIndex.isValid)
                    continue;

                UnlockableIndex requiredUnlockableIndex = UnlockableIndex.None;
                if (survivorDef.unlockableDef)
                {
                    requiredUnlockableIndex = survivorDef.unlockableDef.index;
                }

                IReadOnlyList<ExpansionIndex> requiredExpansions = ExpansionUtils.GetObjectRequiredExpansionIndices(survivorDef.bodyPrefab);

                SpawnPoolEntryParameters entryParameters = new SpawnPoolEntryParameters(1f, [.. requiredExpansions]);

                if (requiredUnlockableIndex != UnlockableIndex.None)
                {
                    entryParameters.IsAvailableFunc = () => !Run.instance || Run.instance.IsUnlockableUnlocked(UnlockableCatalog.GetUnlockableDef(requiredUnlockableIndex));
                }

                _spawnPool.AddEntry(survivorDef, entryParameters);
            }

            _spawnPool.TrimExcess();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _spawnPool.AnyAvailable;
        }

        AssetOrDirectReference<GameObject> _podPrefabReference;

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        List<AssetOrDirectReference<SurvivorDef>> _survivorReferences = [];

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
            _effectComponent.EffectDestructionHandledByComponent = true;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
        }

        IEnumerator Start()
        {
            if (!NetworkServer.active)
                yield break;

            _podPrefabReference = new AssetOrDirectReference<GameObject>
            {
                unloadType = AsyncReferenceHandleUnloadType.AtWill,
                address = new AssetReferenceGameObject(AddressableGuids.RoR2_Base_SurvivorPod_SurvivorPod_prefab)
            };

            while (!_podPrefabReference.IsLoaded())
            {
                yield return null;
            }

            for (int i = _numPodSpawns.Value - 1; i >= 0; i--)
            {
                AssetOrDirectReference<SurvivorDef> survivorRef = _spawnPool.PickRandomEntry(_rng);
                spawnSurvivor(survivorRef, _rng.Branch());
                _survivorReferences.Add(survivorRef);

                if (i > 0)
                {
                    yield return new WaitForSeconds(_rng.RangeFloat(0.1f, 0.3f));
                }
            }

            _effectComponent.EffectDestructionHandledByComponent = false;
        }

        void OnDestroy()
        {
            _podPrefabReference?.Reset();
            _podPrefabReference = null;

            foreach (AssetOrDirectReference<SurvivorDef> survivorRef in _survivorReferences)
            {
                survivorRef?.Reset();
            }

            _survivorReferences.Clear();
        }

        [Server]
        void spawnSurvivor(AssetOrDirectReference<SurvivorDef> survivorReference, Xoroshiro128Plus rng)
        {
            Xoroshiro128Plus spawnRng = new Xoroshiro128Plus(rng.nextUlong);
            survivorReference.CallOnLoaded(survivor =>
            {
                MasterCatalog.MasterIndex masterIndex = MasterCatalog.FindAiMasterIndexForBody(BodyCatalog.FindBodyIndex(survivor.bodyPrefab));
                if (masterIndex.isValid)
                {
                    GameObject masterPrefabObj = MasterCatalog.GetMasterPrefab(masterIndex);

                    CharacterMaster master = new MasterSummon
                    {
                        masterPrefab = masterPrefabObj,
                        position = Vector3.zero,
                        rotation = Quaternion.identity,
                        ignoreTeamMemberLimit = true,
                        teamIndexOverride = TeamIndex.Player,
                        useAmbientLevel = true,
                        loadout = LoadoutUtils.GetRandomLoadoutFor(masterPrefabObj.GetComponent<CharacterMaster>(), spawnRng.Branch())
                    }.Perform();

                    if (!master.hasBody)
                    {
                        master.SpawnBodyHere();
                    }

                    CharacterBody characterBody = master.GetBody();

                    DirectorPlacementRule placementRule = SpawnUtils.GetBestValidRandomPlacementRule();
                    Vector3 spawnPosition = placementRule.EvaluateToPosition(spawnRng.Branch(), HullClassification.Golem, MapNodeGroup.GraphType.Ground, NodeFlags.None, NodeFlags.NoCharacterSpawn);

                    spawnSurvivorPodFor(characterBody, spawnPosition, spawnRng.Branch());
                }
                else
                {
                    Log.Warning($"No AI master found for survivor: {survivor.cachedName}, bodyPrefab={survivor.bodyPrefab}");
                }
            });
        }

        void spawnSurvivorPodFor(CharacterBody survivorBody, Vector3 spawnPosition, Xoroshiro128Plus rng)
        {
            Vector3 podPosition = spawnPosition + new Vector3(0f, 0.75f, 0f);

            Vector3 environmentNormal = SpawnUtils.GetEnvironmentNormalAtPoint(spawnPosition);
            Quaternion podRotation = Quaternion.AngleAxis(rng.RangeFloat(-180f, 180f), environmentNormal) *
                                     QuaternionUtils.PointLocalDirectionAt(Vector3.up, environmentNormal);

            GameObject podObject = Instantiate(_podPrefabReference.WaitForCompletion(), podPosition, podRotation);

            if (!podObject.TryGetComponent(out VehicleSeat vehicleSeat))
            {
                TeleportUtils.TeleportBody(survivorBody, spawnPosition);
                GameObject.Destroy(podObject);

                Log.Warning($"No vehicle seat component on pod prefab {podObject}, teleporting body instead");
                return;
            }

            vehicleSeat.AssignPassenger(survivorBody.gameObject);

            podObject.AddComponent<RepeatedPassengerInteraction>();

            NetworkServer.Spawn(podObject);
        }

        class RepeatedPassengerInteraction : MonoBehaviour
        {
            VehicleSeat _seat;

            float _interactTimer;

            void Awake()
            {
                _seat = GetComponent<VehicleSeat>();
            }

            void FixedUpdate()
            {
                if (!_seat || !NetworkServer.active)
                {
                    Destroy(this);
                    return;
                }

                _interactTimer -= Time.fixedDeltaTime;
                if (_interactTimer <= 0f)
                {
                    _interactTimer = 0.5f;

                    if (_seat.hasPassenger)
                    {
                        CharacterBody passenger = _seat.currentPassengerBody;
                        if (passenger && passenger.TryGetComponent(out Interactor interactor))
                        {
                            interactor.AttemptInteraction(gameObject);
                        }
                    }
                }
            }
        }
    }
}
