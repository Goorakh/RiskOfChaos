using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Audio;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectUtils.World.Spawn
{
    public class BlackHoleController : NetworkBehaviour
    {
        static GameObject _killSphereVFXPrefab;
        static GameObject _environmentVFXPrefab;
        static LoopSoundDef _loopSoundDef;

        static readonly AnimationCurve _growthCurve = new AnimationCurve([
            new Keyframe(0f, 0f, 2f, 2f),
            new Keyframe(1f, 1f, 0f, 0f)
        ]);

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<GameObject> killSphereVfxLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidRaidCrab/KillSphereVfxPlaceholder.prefab");
            killSphereVfxLoad.OnSuccess(p => _killSphereVFXPrefab = p);

            AsyncOperationHandle<GameObject> environmentVfxLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidRaidCrab/VoidRaidCrabSuckLoopFX.prefab");
            environmentVfxLoad.OnSuccess(p => _environmentVFXPrefab = p);

            AsyncOperationHandle<LoopSoundDef> loopSoundLoad = Addressables.LoadAssetAsync<LoopSoundDef>("RoR2/DLC1/VoidRaidCrab/lsdVoidRaidCrabVacuumAttack.asset");
            loopSoundLoad.OnSuccess(l => _loopSoundDef = l);
        }

        [SyncVar]
        RunTimeStamp _startTime = Run.FixedTimeStamp.positiveInfinity;

        [SyncVar]
        public float MaxRadius;

        VFXHelper _killSphereVfxHelper;

        VFXHelper _environmentVfxHelper;

        LoopSoundManager.SoundLoopPtr _loopSound;

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
                    _killSphereVfxHelper.vfxInstanceTransform.localScale = Vector3.one * _currentRadius;
            }
        }

        void Awake()
        {
            _killSphereVfxHelper = VFXHelper.Rent();
            _killSphereVfxHelper.vfxPrefabReference = _killSphereVFXPrefab;
            _killSphereVfxHelper.followedTransform = transform;
            _killSphereVfxHelper.useFollowedTransformScale = false;
            _killSphereVfxHelper.enabled = true;

            _environmentVfxHelper = VFXHelper.Rent();
            _environmentVfxHelper.vfxPrefabReference = _environmentVFXPrefab;
            _environmentVfxHelper.followedTransform = transform;
            _environmentVfxHelper.useFollowedTransformScale = false;
            _environmentVfxHelper.enabled = true;

            if (_loopSoundDef)
                _loopSound = LoopSoundManager.PlaySoundLoopLocal(gameObject, _loopSoundDef);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _startTime = Run.FixedTimeStamp.now;

            _killSearch = new SphereSearch();
        }

        void OnDestroy()
        {
            _killSphereVfxHelper = VFXHelper.Return(_killSphereVfxHelper);
            _environmentVfxHelper = VFXHelper.Return(_environmentVfxHelper);

            LoopSoundManager.StopSoundLoopLocal(_loopSound);
        }

        void FixedUpdate()
        {
            if (!_killSphereVfxHelper.enabled)
                _killSphereVfxHelper.enabled = true;

            if (!_environmentVfxHelper.enabled)
                _environmentVfxHelper.enabled = true;

            float time = Mathf.Clamp01(_startTime.TimeSinceClamped / 10f);
            CurrentRadius = _growthCurve.Evaluate(time) * MaxRadius;

            Vector3 centerPosition = transform.position;

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
                    Vector3 finalMovement = displacement + characterMotor.velocity;
                    if (characterMotor.useGravity)
                        finalMovement += characterMotor.GetGravity();

                    if (finalMovement.y > 0f)
                        characterMotor.Motor.ForceUnground();
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
                        hurtBox.healthComponent.Suicide(null, null, DamageType.VoidDeath);
                }
            }
        }
    }
}