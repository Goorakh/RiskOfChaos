using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.EntityLogic;
using RoR2.Navigation;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_survivors")]
    public sealed class SpawnSurvivors : GenericSpawnEffect<SurvivorDef>, ICoroutineEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static GameObject _podPrefab;

        class SurvivorEntry : SpawnEntry
        {
            public SurvivorEntry(SurvivorDef[] items, float weight) : base(items, weight)
            {
            }

            public SurvivorEntry(SurvivorDef item, float weight) : base(item, weight)
            {
            }

            protected override bool isItemAvailable(SurvivorDef item)
            {
                return base.isItemAvailable(item) && item.CheckRequiredExpansionEnabled() && (!item.unlockableDef || (Run.instance && Run.instance.IsUnlockableUnlocked(item.unlockableDef)));
            }
        }
        static SurvivorEntry[] _survivorEntries;

        [EffectConfig]
        static readonly ConfigHolder<int> _numPodSpawns =
            ConfigFactory<int>.CreateConfig("Pod Spawn Count", 10)
            .Description("The amount of pods to spawn")
            .OptionConfig(new IntSliderConfig
            {
                min = 1,
                max = 20
            })
            .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(1))
            .Build();

        [SystemInitializer(typeof(SurvivorCatalog), typeof(MasterCatalog), typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _podPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/SurvivorPod/SurvivorPod.prefab").WaitForCompletion();

            _survivorEntries = SurvivorCatalog.allSurvivorDefs.Where(s => MasterCatalog.FindAiMasterIndexForBody(BodyCatalog.FindBodyIndex(s.bodyPrefab)).isValid)
                                                              .Select(s => new SurvivorEntry(s, 1f))
                                                              .ToArray();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_survivorEntries);
        }

        public override void OnStart()
        {
        }

        public IEnumerator OnStartCoroutine()
        {
            for (int i = _numPodSpawns.Value - 1; i >= 0; i--)
            {
                spawnSurvivor(getItemToSpawn(_survivorEntries, RNG), new Xoroshiro128Plus(RNG.nextUlong));

                yield return new WaitForSeconds(RNG.RangeFloat(0.1f, 0.3f));
            }
        }

        static void spawnSurvivor(SurvivorDef survivor, Xoroshiro128Plus rng)
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
                    loadout = LoadoutUtils.GetRandomLoadoutFor(masterPrefabObj.GetComponent<CharacterMaster>(), new Xoroshiro128Plus(rng.nextUlong))
                }.Perform();

                if (!master.hasBody)
                {
                    master.SpawnBodyHere();
                }

                CharacterBody characterBody = master.GetBody();

                DirectorPlacementRule placementRule = SpawnUtils.GetBestValidRandomPlacementRule();
                Vector3 spawnPosition = placementRule.EvaluateToPosition(new Xoroshiro128Plus(rng.nextUlong),
                                                                         characterBody.hullClassification,
                                                                         MapNodeGroup.GraphType.Ground,
                                                                         NodeFlags.None,
                                                                         NodeFlags.NoCharacterSpawn);

                spawnSurvivorPodFor(characterBody, spawnPosition, new Xoroshiro128Plus(rng.nextUlong));
            }
            else
            {
                Log.Warning($"No AI master found for survivor: {survivor.cachedName}, bodyPrefab={survivor.bodyPrefab}");
            }
        }

        static void spawnSurvivorPodFor(CharacterBody survivorBody, Vector3 spawnPosition, Xoroshiro128Plus rng)
        {
            GameObject podObject = GameObject.Instantiate(_podPrefab,
                                                          spawnPosition + new Vector3(0f, 0.75f, 0f),
                                                          Quaternion.Euler(0f, rng.RangeFloat(-180f, 180f), 0f));

            if (!podObject.TryGetComponent(out VehicleSeat vehicleSeat))
            {
                TeleportUtils.TeleportBody(survivorBody, spawnPosition);
                GameObject.Destroy(podObject);

                Log.Warning($"No vehicle seat component on pod prefab {podObject}, teleporting body instead");
                return;
            }

            vehicleSeat.AssignPassenger(survivorBody.gameObject);

            DelayedEvent exitPodEvent = vehicleSeat.gameObject.AddComponent<DelayedEvent>();
            exitPodEvent.action = new UnityEngine.Events.UnityEvent();
            exitPodEvent.action.AddListener(() =>
            {
                if (vehicleSeat && vehicleSeat.hasPassenger)
                {
                    CharacterBody passenger = vehicleSeat.currentPassengerBody;
                    if (passenger)
                    {
                        passenger.GetComponent<Interactor>().AttemptInteraction(vehicleSeat.gameObject);
                    }

                    if (exitPodEvent)
                    {
                        exitPodEvent.CallDelayed(0.2f);
                    }
                }
            });
            exitPodEvent.CallDelayed(0.2f);

            NetworkServer.Spawn(podObject);
        }

        public void OnForceStopped()
        {
        }
    }
}
