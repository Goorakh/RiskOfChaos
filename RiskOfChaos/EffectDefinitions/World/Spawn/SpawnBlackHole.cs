using EntityStates.VoidRaidCrab;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Audio;
using RoR2.Navigation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosTimedEffect("spawn_black_hole", 30f, IsNetworked = true)]
    public sealed class SpawnBlackHole : TimedEffect
    {
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

            Vector3 spawnOffset = new Vector3(0f, 65f, 0f);
            Vector3 spawnOffsetDir = spawnOffset.normalized;
            float spawnOffsetCheckDistance = spawnOffset.magnitude;

            DirectorPlacementRule placementRule = SpawnUtils.GetBestValidRandomPlacementRule();

            int failedPositionFindAttempts = 0;
            while (true)
            {
                Vector3 groundPosition = placementRule.EvaluateToPosition(RNG, HullClassification.Golem, MapNodeGroup.GraphType.Ground, NodeFlags.TeleporterOK);

                Ray ray = new Ray(groundPosition, spawnOffsetDir);
                if (!UnityEngine.Physics.Raycast(ray, spawnOffsetCheckDistance, LayerIndex.world.mask.value) || ++failedPositionFindAttempts >= 10)
                {
                    _blackHolePosition = groundPosition + spawnOffset;

#if DEBUG
                    Log.Debug($"Found spawn position after {failedPositionFindAttempts} attempts");
#endif

                    break;
                }
            }
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
            _killSphereVfxHelper.vfxPrefabReference = VacuumAttack.killSphereVfxPrefab;
            _killSphereVfxHelper.followedTransform = _blackHoleOrigin.transform;
            _killSphereVfxHelper.useFollowedTransformScale = false;
            _killSphereVfxHelper.enabled = true;
            updateKillSphereVfx();

            _environmentVfxHelper = VFXHelper.Rent();
            _environmentVfxHelper.vfxPrefabReference = VacuumAttack.environmentVfxPrefab;
            _environmentVfxHelper.followedTransform = _blackHoleOrigin.transform;
            _environmentVfxHelper.useFollowedTransformScale = false;
            _environmentVfxHelper.enabled = true;

            _loopSound = LoopSoundManager.PlaySoundLoopLocal(_blackHoleOrigin, VacuumAttack.loopSound);

            if (NetworkServer.active)
            {
                _killSearch = new SphereSearch();
            }

            RoR2Application.onFixedUpdate += onFixedUpdate;
        }

        void onFixedUpdate()
        {
            float time = Mathf.Clamp01(TimeElapsed / 10f);
            _killRadius = VacuumAttack.killRadiusCurve.Evaluate(time);
            updateKillSphereVfx();

            Vector3 centerPosition = _blackHoleOrigin.transform.position;

            _losTracker.origin = centerPosition;
            _losTracker.Step();

            float pullMagnitude = VacuumAttack.pullMagnitudeCurve.Evaluate(time) * (7.5f / 30f);
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (body.hasEffectiveAuthority)
                {
                    IDisplacementReceiver displacementReceiver = body.GetComponent<IDisplacementReceiver>();
                    if (displacementReceiver != null)
                    {
                        float pullFactor = body.isPlayerControlled ? 1f : 3.5f;
                        displacementReceiver.AddDisplacement((centerPosition - body.footPosition).normalized * (pullMagnitude * pullFactor * Time.fixedDeltaTime));
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
