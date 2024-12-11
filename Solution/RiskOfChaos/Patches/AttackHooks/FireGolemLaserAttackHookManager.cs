using EntityStates.GolemMonster;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    class FireGolemLaserAttackHookManager : AttackHookManager
    {
        const float MAX_DISTANCE = 1000f;

        protected override AttackInfo AttackInfo { get; }

        public FireGolemLaserAttackHookManager(FireLaser fireLaserState)
        {
            AttackInfo = new AttackInfo(fireLaserState);
        }

        protected override void fireAttackCopy()
        {
            AttackInfo attackInfo = AttackInfo;

            Util.PlaySound(FireLaser.attackSoundString, attackInfo.Attacker);

            Ray laserRay = new Ray(attackInfo.Position, attackInfo.AttackDirection);

            Vector3 laserEndPosition = laserRay.GetPoint(MAX_DISTANCE);
            if (Util.CharacterRaycast(attackInfo.Attacker, laserRay, out RaycastHit raycastHit, MAX_DISTANCE, LayerIndex.CommonMasks.laser, QueryTriggerInteraction.UseGlobal))
            {
                laserEndPosition = raycastHit.point;
            }

            new BlastAttack
            {
                attacker = attackInfo.Attacker,
                inflictor = attackInfo.Attacker,
                teamIndex = TeamComponent.GetObjectTeam(attackInfo.Attacker),
                baseDamage = attackInfo.Damage,
                baseForce = attackInfo.Force * 0.2f,
                position = laserEndPosition,
                radius = FireLaser.blastRadius,
                falloffModel = BlastAttack.FalloffModel.SweetSpot,
                bonusForce = attackInfo.Force * laserRay.direction
            }.Fire();

            if (FireLaser.tracerEffectPrefab)
            {
                EffectData effectData = new EffectData
                {
                    origin = laserEndPosition,
                    start = attackInfo.MuzzlePosition
                };

                EffectManager.SpawnEffect(FireLaser.tracerEffectPrefab, effectData, true);
                EffectManager.SpawnEffect(FireLaser.hitEffectPrefab, effectData, true);
            }
        }

        protected override bool tryFireBounce(AttackHookMask activeAttackHooks)
        {
            BulletAttack bulletAttack = new BulletAttack();
            AttackInfo.PopulateBulletAttack(bulletAttack);

            bulletAttack.allowTrajectoryAimAssist = false;

            bulletAttack.hitMask = LayerIndex.CommonMasks.laser;
            bulletAttack.stopperMask = LayerIndex.CommonMasks.laser;

            bulletAttack.maxDistance = MAX_DISTANCE;

            // HACK: Prevent duplicate damage before bounce
            float damage = bulletAttack.damage;
            bulletAttack.damage = 0f;

            BulletAttack.HitCallback origHitCallback = bulletAttack.hitCallback;
            bulletAttack.hitCallback = hitCallback;
            bool hitCallback(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
            {
                bool stopBullet = false;

                bool isAfterFirstBounce = bulletAttack.damage > 0f;

                if (isAfterFirstBounce)
                {
                    stopBullet = origHitCallback(bulletAttack, ref hitInfo);

                    new BlastAttack
                    {
                        attacker = bulletAttack.owner,
                        inflictor = bulletAttack.weapon,
                        teamIndex = TeamComponent.GetObjectTeam(bulletAttack.owner),
                        baseDamage = bulletAttack.damage,
                        baseForce = bulletAttack.force * 0.2f,
                        position = hitInfo.point,
                        radius = FireLaser.blastRadius,
                        falloffModel = BlastAttack.FalloffModel.SweetSpot,
                        bonusForce = bulletAttack.force * hitInfo.direction,
                        procChainMask = bulletAttack.procChainMask,
                        procCoefficient = bulletAttack.procCoefficient
                    }.Fire();

                    bulletAttack.tracerEffectPrefab = FireLaser.tracerEffectPrefab;
                    bulletAttack.hitEffectPrefab = FireLaser.hitEffectPrefab;
                }
                else
                {
                    bulletAttack.damage = damage;
                }

                return stopBullet;
            }

            if (!BulletBounceHook.TryStartBounce(bulletAttack, AttackInfo.AttackDirection, 0, activeAttackHooks))
                return false;

            bulletAttack.Fire();
            return true;
        }
    }
}
