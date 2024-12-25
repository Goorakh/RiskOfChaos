using EntityStates.GolemMonster;
using HG;
using RoR2;
using UnityEngine;
using static UnityEngine.SendMouseEvents;

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

        protected override void fireAttackCopy(AttackInfo attackInfo)
        {
            Util.PlaySound(FireLaser.attackSoundString, attackInfo.Attacker);

            Ray laserRay = new Ray(attackInfo.Position, attackInfo.AttackDirection);

            Vector3 laserEndPosition = laserRay.GetPoint(MAX_DISTANCE);
            if (Util.CharacterRaycast(attackInfo.Attacker, laserRay, out RaycastHit raycastHit, MAX_DISTANCE, LayerIndex.CommonMasks.laser, QueryTriggerInteraction.UseGlobal))
            {
                laserEndPosition = raycastHit.point;
            }

            BlastAttack blastAttack = new BlastAttack();
            attackInfo.PopulateBlastAttack(blastAttack);
            blastAttack.attackerFiltering = AttackerFiltering.Default;
            blastAttack.falloffModel = BlastAttack.FalloffModel.SweetSpot;
            blastAttack.baseForce = attackInfo.Force * 0.2f;
            blastAttack.radius = FireLaser.blastRadius;
            blastAttack.Fire();

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

        protected override bool tryFireBounce()
        {
            AttackInfo attackInfo = AttackInfo;

            BulletAttack bulletAttack = new BulletAttack();
            attackInfo.PopulateBulletAttack(bulletAttack);

            bulletAttack.allowTrajectoryAimAssist = false;

            bulletAttack.hitMask = LayerIndex.CommonMasks.laser;
            bulletAttack.stopperMask = LayerIndex.CommonMasks.laser;

            bulletAttack.maxDistance = MAX_DISTANCE;

            bulletAttack.damage = 0f;
            bulletAttack.damageType = DamageTypeCombo.Generic;
            bulletAttack.isCrit = false;
            bulletAttack.force = 0f;
            bulletAttack.procCoefficient = 0f;

            void onBounceHit(BulletAttack bulletAttack, BulletBounceHook.BulletBounceInfo bounceInfo)
            {
                BulletAttack.BulletHit bounceHit = bounceInfo.LastHit;

                if (bounceInfo.BouncesCompleted >= 2)
                {
                    if (bounceHit != null)
                    {
                        BlastAttack blastAttack = new BlastAttack();
                        attackInfo.PopulateBlastAttack(blastAttack);
                        blastAttack.radius = FireLaser.blastRadius;
                        blastAttack.falloffModel = BlastAttack.FalloffModel.SweetSpot;
                        blastAttack.position = bounceHit.point;
                        blastAttack.baseForce = attackInfo.Force * 0.2f;
                        blastAttack.bonusForce = attackInfo.Force * bounceHit.direction;

                        blastAttack.Fire();

#if DEBUG
                        GameObject sphereIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        sphereIndicator.transform.position = blastAttack.position;
                        sphereIndicator.transform.localScale = Vector3.one * (blastAttack.radius / 2f);
                        sphereIndicator.GetComponent<Collider>().enabled = false;

                        DestroyOnTimer sphereTimer = sphereIndicator.AddComponent<DestroyOnTimer>();
                        sphereTimer.duration = 5f;
#endif
                    }
                }

                if (bounceInfo.BouncesCompleted >= 1)
                {
                    bulletAttack.tracerEffectPrefab = FireLaser.tracerEffectPrefab;
                    bulletAttack.hitEffectPrefab = FireLaser.hitEffectPrefab;
                }
            }

            if (!BulletBounceHook.TryStartBounce(bulletAttack, attackInfo, onBounceHit))
                return false;

            bulletAttack.Fire();
            return true;
        }
    }
}
