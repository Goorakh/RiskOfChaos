using RiskOfChaos.Collections;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.Projectile;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("poison_trail", 60f)]
    public class PoisonTrail : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _poisonTrailMaxSegmentCount =
            ConfigFactory<int>.CreateConfig("Max Trail Length", 20)
                              .Description("The maximum number of active trail segments per character.")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        static GameObject _trailSegmentPrefab;

        [SystemInitializer(typeof(ProjectileCatalog))]
        static void Init()
        {
            _trailSegmentPrefab = ProjectileCatalog.GetProjectilePrefab(ProjectileCatalog.FindProjectileIndex("PoisonTrailSegment"));
            if (!_trailSegmentPrefab)
            {
                Log.Error("Failed to find poison trail segment projectile prefab");
            }
        }

        [ContentInitializer]
        static IEnumerator LoadContent(ProjectilePrefabAssetCollection projectilePrefabs)
        {
            AsyncOperationHandle<GameObject> poisonPoolLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoLeapAcid.prefab");
            poisonPoolLoad.OnSuccess(poisonPoolPrefab =>
            {
                GameObject trailSegmentPrefab = poisonPoolPrefab.InstantiateNetworkedPrefab("PoisonTrailSegment");

                Deployable deployable = trailSegmentPrefab.AddComponent<Deployable>();
                deployable.onUndeploy = new UnityEvent();
                deployable.onUndeploy.m_PersistentCalls.AddListener(new PersistentCall
                {
                    m_Target = deployable,
                    m_MethodName = nameof(Deployable.DestroyGameObject),
                    mode = PersistentListenerMode.Void
                });

                ProjectileDeployToOwner deployToOwner = trailSegmentPrefab.AddComponent<ProjectileDeployToOwner>();
                deployToOwner.deployableSlot = DeployableSlots.PoisonTrailSegment;

                projectilePrefabs.Add(trailSegmentPrefab);
            });

            AsyncOperationHandle[] asyncOperations = [poisonPoolLoad];
            yield return asyncOperations.WaitForAllLoaded();
        }

        public static int GetPoisonTrailSegmentLimit(CharacterMaster master)
        {
            return _poisonTrailMaxSegmentCount.Value;
        }

        readonly ClearingObjectList<PeriodicPoisonPoolPlacer> _poisonPoolPlacers = [];

        void Start()
        {
            _poisonPoolPlacers.EnsureCapacity(CharacterBody.readOnlyInstancesList.Count);

            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                handleBody(body);
            }

            CharacterBody.onBodyStartGlobal += handleBody;
        }

        void OnDestroy()
        {
            CharacterBody.onBodyStartGlobal -= handleBody;

            _poisonPoolPlacers.ClearAndDispose(true);
        }

        void handleBody(CharacterBody body)
        {
            if (body && body.hasEffectiveAuthority)
            {
                PeriodicPoisonPoolPlacer poisonPoolPlacer = body.gameObject.AddComponent<PeriodicPoisonPoolPlacer>();
                _poisonPoolPlacers.Add(poisonPoolPlacer);
            }
        }

        class PeriodicPoisonPoolPlacer : MonoBehaviour
        {
            CharacterBody _body;

            Vector3 _lastSegmentPosition;
            float _lastSegmentTimer;

            public float SegmentMaxDistance = 8f;

            public float SegmentMaxDuration = 9f;

            void Awake()
            {
                _body = GetComponent<CharacterBody>();
            }

            void FixedUpdate()
            {
                if (_body && _body.hasEffectiveAuthority)
                {
                    fixedUpdateAuthority();
                }
            }

            void fixedUpdateAuthority()
            {
                float segmentMaxDistance = SegmentMaxDistance * Ease.InCubic(Mathf.Clamp01(_lastSegmentTimer / SegmentMaxDuration));
                Vector3 currentPlacementPosition = _body.footPosition;

                _lastSegmentTimer -= Time.fixedDeltaTime;

                if (_lastSegmentTimer <= 0f || (_lastSegmentPosition - currentPlacementPosition).sqrMagnitude >= segmentMaxDistance * segmentMaxDistance)
                {
                    _lastSegmentTimer = SegmentMaxDuration;
                    _lastSegmentPosition = currentPlacementPosition;

                    Vector3 viewDirection = _body.transform.forward;
                    if (_body.characterDirection)
                    {
                        viewDirection = _body.characterDirection.forward;
                    }

                    ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                    {
                        projectilePrefab = _trailSegmentPrefab,
                        position = currentPlacementPosition,
                        rotation = Util.QuaternionSafeLookRotation(new Vector3(0f, viewDirection.y, 0f).normalized),
                        owner = _body.gameObject,
                        crit = _body.RollCrit(),
                        damage = _body.damage
                    });
                }
            }
        }
    }
}
