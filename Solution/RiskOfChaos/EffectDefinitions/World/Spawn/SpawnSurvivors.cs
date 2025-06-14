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
using RoR2.Navigation;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_survivors")]
    public sealed class SpawnSurvivors : NetworkBehaviour
    {
        static readonly SpawnPool<SurvivorDef> _spawnPool = new SpawnPool<SurvivorDef>
        {
            RequiredExpansionsProvider = survivorDef => ExpansionUtils.GetObjectRequiredExpansions(survivorDef.bodyPrefab)
        };

        [EffectConfig]
        static readonly ConfigHolder<int> _numPodSpawns =
            ConfigFactory<int>.CreateConfig("Pod Spawn Count", 10)
                              .Description("The amount of pods to spawn")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        [SystemInitializer(typeof(SurvivorCatalog), typeof(MasterCatalog))]
        static void Init()
        {
            _spawnPool.CalcIsEntryAvailable += survivor =>
            {
                return !survivor.unlockableDef || (Run.instance && Run.instance.IsUnlockableUnlocked(survivor.unlockableDef));
            };

            _spawnPool.EnsureCapacity(SurvivorCatalog.survivorCount);

            foreach (SurvivorDef survivorDef in SurvivorCatalog.allSurvivorDefs)
            {
                MasterCatalog.MasterIndex aiMasterIndex = MasterCatalog.FindAiMasterIndexForBody(BodyCatalog.FindBodyIndex(survivorDef.bodyPrefab));
                if (!aiMasterIndex.isValid)
                    continue;

                _spawnPool.AddEntry(new SpawnPool<SurvivorDef>.Entry(survivorDef, new SpawnPoolEntryParameters(1f)));
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
                spawnSurvivor(_spawnPool.PickRandomEntry(_rng), _rng.Branch());

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
        }

        void spawnSurvivor(SurvivorDef survivor, Xoroshiro128Plus rng)
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
                    loadout = LoadoutUtils.GetRandomLoadoutFor(masterPrefabObj.GetComponent<CharacterMaster>(), rng.Branch())
                }.Perform();

                if (!master.hasBody)
                {
                    master.SpawnBodyHere();
                }

                CharacterBody characterBody = master.GetBody();

                DirectorPlacementRule placementRule = SpawnUtils.GetBestValidRandomPlacementRule();
                Vector3 spawnPosition = placementRule.EvaluateToPosition(rng.Branch(), HullClassification.Golem, MapNodeGroup.GraphType.Ground, NodeFlags.None, NodeFlags.NoCharacterSpawn);

                spawnSurvivorPodFor(characterBody, spawnPosition, rng.Branch());
            }
            else
            {
                Log.Warning($"No AI master found for survivor: {survivor.cachedName}, bodyPrefab={survivor.bodyPrefab}");
            }
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
