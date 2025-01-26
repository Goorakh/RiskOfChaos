using EntityStates;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.Content.EntityStates.PulseGolem
{
    [EntityStateType]
    public class FireHook : BaseState
    {
        static GameObject _muzzleEffectPrefab;

        [SystemInitializer]
        static void Init()
        {
            Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Gravekeeper/MuzzleflashWinch.prefab").OnSuccess(muzzleEffectPrefab =>
            {
                _muzzleEffectPrefab = muzzleEffectPrefab;
            });
        }

        static readonly float _baseDuration = 2f;

        static readonly string _attackSoundString = "Play_gravekeeper_attack2_shoot";

        static readonly int _fireLaserAnimationStateHash = Animator.StringToHash("FireLaser");

        static readonly int _fireLaserAnimationPlaybackRateParamHash = Animator.StringToHash("FireLaser.playbackRate");

        public Ray? HookFireRay;

        float _duration;

        public override void OnEnter()
        {
            base.OnEnter();

            HookFireRay ??= GetAimRay();

            _duration = _baseDuration / attackSpeedStat;

            Util.PlaySound(_attackSoundString, gameObject);

            if (characterBody)
            {
                characterBody.SetAimTimer(2f);
            }

            PlayAnimation("Gesture", _fireLaserAnimationStateHash, _fireLaserAnimationPlaybackRateParamHash, _duration);

            string laserMuzzleName = "MuzzleLaser";

            if (_muzzleEffectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(_muzzleEffectPrefab, gameObject, laserMuzzleName, false);
            }

            if (isAuthority)
            {
                Ray aimRay = HookFireRay.Value;

                if (RoCContent.ProjectilePrefabs.PulseGolemHookProjectile)
                {
                    ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                    {
                        projectilePrefab = RoCContent.ProjectilePrefabs.PulseGolemHookProjectile,
                        owner = gameObject,
                        damage = damageStat * 1.5f,
                        force = 1000f,
                        position = aimRay.origin,
                        rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                        crit = RollCrit(),
                        damageTypeOverride = DamageTypeCombo.GenericSecondary
                    });
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (fixedAge >= _duration)
            {
                if (isAuthority)
                {
                    outer.SetNextStateToMain();
                }

                return;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
