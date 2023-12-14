using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Audio;
using RoR2.Navigation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosTimedEffect("spawn_black_hole", 30f, IsNetworked = true)]
    public sealed class SpawnBlackHole : TimedEffect
    {
        const float MAX_RADIUS = 50f;

        static readonly AnimationCurve _growthCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0f, 0f, 2f, 2f),
            new Keyframe(1f, 1f, 0f, 0f)
        });

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
        GameObject _blackHoleOrigin;

        CharacterLosTracker _losTracker;

        VFXHelper _killSphereVfxHelper;

        VFXHelper _environmentVfxHelper;

        SphereSearch _killSearch;
        float _killRadius = 1f;

        LoopSoundManager.SoundLoopPtr _loopSound;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            Vector3 spawnOffset = new Vector3(0f, MAX_RADIUS + 12.5f, 0f);

            DirectorPlacementRule placementRule = SpawnUtils.GetBestValidRandomPlacementRule();

            Collider[] spawnPositionOverlaps = new Collider[byte.MaxValue];

            Vector3 bestEncounteredSpawnPosition = Vector3.zero;
            float bestSpawnPositionSqrFitRadius = 0f;

            const int MAX_SPAWN_POSITION_CANDIDATE_COUNT = 25;

            for (int i = 0; i < MAX_SPAWN_POSITION_CANDIDATE_COUNT; i++)
            {
                Vector3 groundPosition = placementRule.EvaluateToPosition(RNG, HullClassification.Golem, MapNodeGroup.GraphType.Ground);
                Vector3 spawnPosition = groundPosition + spawnOffset;

                int overlapCount = UnityEngine.Physics.OverlapSphereNonAlloc(spawnPosition, MAX_RADIUS, spawnPositionOverlaps, LayerIndex.world.mask.value);

                // No overlap: It fits completely, no need to check any more positions
                if (overlapCount == 0)
                {
#if DEBUG
                    Log.Debug($"Candidate {i} overlaps 0 objects, selecting");
#endif

                    bestEncounteredSpawnPosition = spawnPosition;
                    bestSpawnPositionSqrFitRadius = MAX_RADIUS * MAX_RADIUS;
                    break;
                }

                float sqrFitRadius = spawnPositionOverlaps.Take(overlapCount)
                                                          .Select(c =>
                                                          {
                                                              Vector3 closestPoint;
                                                              switch (c)
                                                              {
                                                                  case BoxCollider:
                                                                  case SphereCollider:
                                                                  case CapsuleCollider:
                                                                  case MeshCollider meshCollider when meshCollider.convex:
                                                                      closestPoint = c.ClosestPoint(spawnPosition);
                                                                      break;
                                                                  default:
                                                                      closestPoint = c.ClosestPointOnBounds(spawnPosition);
                                                                      break;
                                                              }

                                                              return (closestPoint - spawnPosition).sqrMagnitude;
                                                          })
                                                          .Min();

#if DEBUG
                float fitRadius = Mathf.Sqrt(sqrFitRadius);
                Log.Debug($"Candidate {i} overlaps {overlapCount} object(s) ({MAX_RADIUS - fitRadius} units, {fitRadius / MAX_RADIUS:P} fit): [{string.Join(", ", spawnPositionOverlaps.Take(overlapCount))}]");
#endif

                if (sqrFitRadius > bestSpawnPositionSqrFitRadius)
                {
                    bestSpawnPositionSqrFitRadius = sqrFitRadius;
                    bestEncounteredSpawnPosition = spawnPosition;
                }
            }

#if DEBUG
            float bestSpawnPositionFitRadius = Mathf.Sqrt(bestSpawnPositionSqrFitRadius);
            Log.Debug($"Selected spawn position with {MAX_RADIUS - bestSpawnPositionFitRadius} units overlap ({bestSpawnPositionFitRadius / MAX_RADIUS:P} fit)");
#endif

            _blackHolePosition = bestEncounteredSpawnPosition;
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.Write(_blackHolePosition);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            _blackHolePosition = reader.ReadVector3();
        }

        public override void OnStart()
        {
            _blackHoleOrigin = new GameObject();
            _blackHoleOrigin.AddComponent<SetDontDestroyOnLoad>();
            _blackHoleOrigin.AddComponent<AkGameObj>();
            _blackHoleOrigin.transform.position = _blackHolePosition;

            _losTracker = new CharacterLosTracker();
            _losTracker.enabled = true;

            _killSphereVfxHelper = VFXHelper.Rent();
            _killSphereVfxHelper.vfxPrefabReference = _killSphereVFXPrefab;
            _killSphereVfxHelper.followedTransform = _blackHoleOrigin.transform;
            _killSphereVfxHelper.useFollowedTransformScale = false;
            _killSphereVfxHelper.enabled = true;
            updateKillSphereVfx();

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
            _killRadius = _growthCurve.Evaluate(time) * MAX_RADIUS;
            updateKillSphereVfx();

            Vector3 centerPosition = _blackHoleOrigin.transform.position;

            _losTracker.origin = centerPosition;
            _losTracker.Step();

            float pullMagnitude = _growthCurve.Evaluate(time) * 8.5f;
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (body.hasEffectiveAuthority)
                {
                    IDisplacementReceiver displacementReceiver = body.GetComponent<IDisplacementReceiver>();
                    if (displacementReceiver != null)
                    {
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
                }
            }

            if (NetworkServer.active)
            {
                _killSearch.origin = centerPosition;
                _killSearch.radius = _killRadius;
                _killSearch.mask = LayerIndex.entityPrecise.mask;

                _killSearch.RefreshCandidates();
                _killSearch.FilterCandidatesByDistinctHurtBoxEntities();

                List<HurtBox> hurtBoxes = new List<HurtBox>();
                _killSearch.GetHurtBoxes(hurtBoxes);

                foreach (HurtBox hurtBox in hurtBoxes)
                {
                    if (hurtBox.healthComponent)
                    {
                        hurtBox.healthComponent.Suicide(null, null, DamageType.VoidDeath);
                    }
                }
            }
        }

        void updateKillSphereVfx()
        {
            if (_killSphereVfxHelper.vfxInstanceTransform)
            {
                _killSphereVfxHelper.vfxInstanceTransform.localScale = Vector3.one * _killRadius;
            }
        }

        public override void OnEnd()
        {
            _killSphereVfxHelper = VFXHelper.Return(_killSphereVfxHelper);
            _environmentVfxHelper = VFXHelper.Return(_environmentVfxHelper);

            _losTracker.enabled = false;
            _losTracker.Dispose();
            _losTracker = null;

            LoopSoundManager.StopSoundLoopLocal(_loopSound);

            GameObject.Destroy(_blackHoleOrigin);

            RoR2Application.onFixedUpdate -= onFixedUpdate;
        }
    }
}
