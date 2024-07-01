using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.Audio;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosTimedEffect("spawn_black_hole", 30f, IsNetworked = true)]
    public sealed class SpawnBlackHole : TimedEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _maxRadiusConfig =
            ConfigFactory<float>.CreateConfig("Size", 50f)
                                .Description("The size of the black hole")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new FloatFieldConfig { Min = 0f })
                                .Build();

        static readonly AnimationCurve _growthCurve = new AnimationCurve([
            new Keyframe(0f, 0f, 2f, 2f),
            new Keyframe(1f, 1f, 0f, 0f)
        ]);

        static GameObject _killSphereVFXPrefab;
        static GameObject _environmentVFXPrefab;
        static LoopSoundDef _loopSoundDef;

        [SystemInitializer]
        static void Init()
        {
            _killSphereVFXPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidRaidCrab/KillSphereVfxPlaceholder.prefab").WaitForCompletion();
            _environmentVFXPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidRaidCrab/VoidRaidCrabSuckLoopFX.prefab").WaitForCompletion();
            _loopSoundDef = Addressables.LoadAssetAsync<LoopSoundDef>("RoR2/DLC1/VoidRaidCrab/lsdVoidRaidCrabVacuumAttack.asset").WaitForCompletion();
        }

        Vector3 _blackHolePosition;
        float _blackHoleRadius;

        GameObject _blackHoleOrigin;

        VFXHelper _killSphereVfxHelper;

        VFXHelper _environmentVfxHelper;

        SphereSearch _killSearch;

        float _currentRadius;
        public float CurrentRadius
        {
            get
            {
                return _currentRadius;
            }
            private set
            {
                _currentRadius = value;

                if (_killSphereVfxHelper.vfxInstanceTransform)
                {
                    _killSphereVfxHelper.vfxInstanceTransform.localScale = Vector3.one * value;
                }
            }
        }

        LoopSoundManager.SoundLoopPtr _loopSound;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            float maxRadius = _maxRadiusConfig.Value;

            Vector3 spawnOffset = new Vector3(0f, maxRadius + 12.5f, 0f);

            DirectorPlacementRule placementRule = SpawnUtils.GetBestValidRandomPlacementRule();

            Collider[] spawnPositionOverlapsBuffer = new Collider[32];

            Vector3 bestEncounteredSpawnPosition = Vector3.zero;
            float bestSpawnPositionSqrFitRadius = 0f;

            const int MAX_SPAWN_POSITION_CANDIDATE_COUNT = 25;

            for (int i = 0; i < MAX_SPAWN_POSITION_CANDIDATE_COUNT; i++)
            {
                Vector3 groundPosition = placementRule.EvaluateToPosition(RNG, HullClassification.Golem, MapNodeGroup.GraphType.Ground);
                Vector3 spawnPosition = groundPosition + spawnOffset;

                int overlapCount = Physics.OverlapSphereNonAlloc(spawnPosition, maxRadius, spawnPositionOverlapsBuffer, LayerIndex.world.mask.value);

                // No overlap: It fits completely, no need to check any more positions
                if (overlapCount == 0)
                {
#if DEBUG
                    Log.Debug($"Candidate {i} overlaps 0 objects, selecting");
#endif

                    bestEncounteredSpawnPosition = spawnPosition;
                    bestSpawnPositionSqrFitRadius = maxRadius * maxRadius;
                    break;
                }

                float closestOverlapPointSqrDistance = float.PositiveInfinity;
                for (int j = 0; j < overlapCount; j++)
                {
                    Collider overlapCollider = spawnPositionOverlapsBuffer[j];

                    Vector3 closestPoint;
                    switch (overlapCollider)
                    {
                        case BoxCollider:
                        case SphereCollider:
                        case CapsuleCollider:
                        case MeshCollider meshCollider when meshCollider.convex:
                            closestPoint = overlapCollider.ClosestPoint(spawnPosition);
                            break;
                        default:
                            closestPoint = overlapCollider.ClosestPointOnBounds(spawnPosition);
                            break;
                    }

                    closestOverlapPointSqrDistance = Mathf.Min(closestOverlapPointSqrDistance, (closestPoint - spawnPosition).sqrMagnitude);
                }

#if DEBUG
                float fitRadius = Mathf.Sqrt(closestOverlapPointSqrDistance);

                Collider[] overlaps = new Collider[overlapCount];
                System.Array.Copy(spawnPositionOverlapsBuffer, overlaps, overlapCount);

                Log.Debug($"Candidate {i} overlaps {overlapCount} object(s) ({maxRadius - fitRadius} units, {fitRadius / maxRadius:P} fit): [{string.Join<Collider>(", ", overlaps)}]");
#endif

                if (closestOverlapPointSqrDistance > bestSpawnPositionSqrFitRadius)
                {
                    bestSpawnPositionSqrFitRadius = closestOverlapPointSqrDistance;
                    bestEncounteredSpawnPosition = spawnPosition;
                }
            }

#if DEBUG
            float bestSpawnPositionFitRadius = Mathf.Sqrt(bestSpawnPositionSqrFitRadius);
            Log.Debug($"Selected spawn position with {maxRadius - bestSpawnPositionFitRadius} units overlap ({bestSpawnPositionFitRadius / maxRadius:P} fit)");
#endif

            _blackHoleRadius = maxRadius;
            _blackHolePosition = bestEncounteredSpawnPosition;
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.Write(_blackHolePosition);
            writer.Write(_blackHoleRadius);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            _blackHolePosition = reader.ReadVector3();
            _blackHoleRadius = reader.ReadSingle();
        }

        public override void OnStart()
        {
            _blackHoleOrigin = new GameObject();
            _blackHoleOrigin.AddComponent<SetDontDestroyOnLoad>();
            _blackHoleOrigin.AddComponent<AkGameObj>();
            _blackHoleOrigin.transform.position = _blackHolePosition;

            _killSphereVfxHelper = VFXHelper.Rent();
            _killSphereVfxHelper.vfxPrefabReference = _killSphereVFXPrefab;
            _killSphereVfxHelper.followedTransform = _blackHoleOrigin.transform;
            _killSphereVfxHelper.useFollowedTransformScale = false;
            _killSphereVfxHelper.enabled = true;

            _environmentVfxHelper = VFXHelper.Rent();
            _environmentVfxHelper.vfxPrefabReference = _environmentVFXPrefab;
            _environmentVfxHelper.followedTransform = _blackHoleOrigin.transform;
            _environmentVfxHelper.useFollowedTransformScale = false;
            _environmentVfxHelper.enabled = true;

            if (_loopSoundDef)
            {
                _loopSound = LoopSoundManager.PlaySoundLoopLocal(_blackHoleOrigin, _loopSoundDef);
            }

            if (NetworkServer.active)
            {
                _killSearch = new SphereSearch();
            }

            RoR2Application.onFixedUpdate += onFixedUpdate;
        }

        void onFixedUpdate()
        {
            if (!_killSphereVfxHelper.enabled)
            {
                _killSphereVfxHelper.enabled = true;
            }

            if (!_environmentVfxHelper.enabled)
            {
                _environmentVfxHelper.enabled = true;
            }

            float time = Mathf.Clamp01(TimeElapsed / 10f);
            CurrentRadius = _growthCurve.Evaluate(time) * _blackHoleRadius;

            Vector3 centerPosition = _blackHoleOrigin.transform.position;

            float pullMagnitude = _growthCurve.Evaluate(time) * 8.5f;
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (!body.hasEffectiveAuthority)
                    continue;

                IDisplacementReceiver displacementReceiver = body.GetComponent<IDisplacementReceiver>();
                if (displacementReceiver is null)
                    continue;

                float pullFactor = body.isPlayerControlled ? 1f : 5f;

                float displacementStrength = pullMagnitude * pullFactor;
                Vector3 displacement = (centerPosition - body.footPosition).normalized * displacementStrength;

                displacementReceiver.AddDisplacement(displacement * Time.fixedDeltaTime);

                CharacterMotor characterMotor = body.characterMotor;
                if (characterMotor && characterMotor.Motor)
                {
                    Vector3 finalMovement;
                    if (characterMotor.useGravity)
                    {
                        finalMovement = displacement + characterMotor.GetGravity();
                    }
                    else
                    {
                        finalMovement = displacement;
                    }

                    if (finalMovement.y > 0f)
                    {
                        characterMotor.Motor.ForceUnground();
                    }
                }
            }

            if (NetworkServer.active)
            {
                _killSearch.origin = centerPosition;
                _killSearch.radius = CurrentRadius;
                _killSearch.mask = LayerIndex.entityPrecise.mask;

                _killSearch.RefreshCandidates();
                _killSearch.FilterCandidatesByDistinctHurtBoxEntities();

                foreach (HurtBox hurtBox in _killSearch.GetHurtBoxes())
                {
                    if (hurtBox.healthComponent)
                    {
                        hurtBox.healthComponent.Suicide(null, null, DamageType.VoidDeath);
                    }
                }
            }
        }

        public override void OnEnd()
        {
            _killSphereVfxHelper = VFXHelper.Return(_killSphereVfxHelper);
            _environmentVfxHelper = VFXHelper.Return(_environmentVfxHelper);

            LoopSoundManager.StopSoundLoopLocal(_loopSound);

            GameObject.Destroy(_blackHoleOrigin);

            RoR2Application.onFixedUpdate -= onFixedUpdate;
        }
    }
}
