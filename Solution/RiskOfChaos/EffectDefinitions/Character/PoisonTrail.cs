using RiskOfChaos.Collections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("poison_trail", 60f)]
    public class PoisonTrail : MonoBehaviour
    {
        static GameObject _poisonPoolPrefab;

        [SystemInitializer(typeof(ProjectileCatalog))]
        static void Init()
        {
            _poisonPoolPrefab = ProjectileCatalog.GetProjectilePrefab(ProjectileCatalog.FindProjectileIndex("CrocoLeapAcid"));
            if (!_poisonPoolPrefab)
            {
                Log.Error("Failed to find poison pool projectile prefab");
            }
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

            public float SegmentMaxDistance = 7f;

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

                    viewDirection.x = 0f;
                    viewDirection.z = 0f;

                    ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                    {
                        projectilePrefab = _poisonPoolPrefab,
                        position = currentPlacementPosition,
                        rotation = Util.QuaternionSafeLookRotation(viewDirection),
                        owner = _body.gameObject,
                        crit = _body.RollCrit(),
                        damage = _body.damage
                    });
                }
            }
        }
    }
}
