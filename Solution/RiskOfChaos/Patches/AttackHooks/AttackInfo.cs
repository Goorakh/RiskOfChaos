﻿using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos_PatcherInterop;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    public struct AttackInfo
    {
        public GameObject Attacker;
        public GameObject Inflictor;
        public GameObject Target;
        public Vector3 Position;
        public Vector3 AttackDirection;
        public float Damage;
        public float Force;
        public bool Crit;
        public DamageColorIndex DamageColorIndex;
        public ProcChainMask ProcChainMask;
        public DamageTypeCombo? DamageTypeOverride;
        public float ProcCoefficient;
        public float? Speed;

        Vector3? _muzzlePosition;
        public Vector3 MuzzlePosition
        {
            readonly get
            {
                return _muzzlePosition.GetValueOrDefault(Position);
            }
            set
            {
                _muzzlePosition = value;
            }
        }

        public AttackInfo(in FireProjectileInfo fireProjectileInfo)
        {
            Vector3 position = fireProjectileInfo.position;

            Attacker = fireProjectileInfo.owner;
            Target = fireProjectileInfo.target;
            Position = position;
            MuzzlePosition = position;
            AttackDirection = fireProjectileInfo.rotation * Vector3.forward;
            Damage = fireProjectileInfo.damage;
            Force = fireProjectileInfo.force;
            Crit = fireProjectileInfo.crit;
            DamageColorIndex = fireProjectileInfo.damageColorIndex;
            ProcChainMask = fireProjectileInfo.procChainMask;
            DamageTypeOverride = fireProjectileInfo.damageTypeOverride;

            float procCoefficient = 0f;
            float? procCoefficientOverride = fireProjectileInfo.GetProcCoefficientOverride();
            if (procCoefficientOverride.HasValue)
            {
                procCoefficient = procCoefficientOverride.Value;
            }
            else if (fireProjectileInfo.projectilePrefab && fireProjectileInfo.projectilePrefab.TryGetComponent(out ProjectileController projectileController))
            {
                procCoefficient = projectileController.procCoefficient;
            }

            ProcCoefficient = procCoefficient;

            Speed = fireProjectileInfo.useSpeedOverride ? fireProjectileInfo.speedOverride : null;
        }

        public AttackInfo(BlastAttack blastAttack)
        {
            Vector3 position = blastAttack.position;

            Attacker = blastAttack.attacker;
            Inflictor = blastAttack.inflictor;
            Target = null;
            Position = position;
            MuzzlePosition = position;
            AttackDirection = Vector3.zero;
            Damage = blastAttack.baseDamage;
            Force = blastAttack.baseForce;
            Crit = blastAttack.crit;
            DamageColorIndex = blastAttack.damageColorIndex;
            ProcChainMask = blastAttack.procChainMask;
            DamageTypeOverride = blastAttack.damageType;
            ProcCoefficient = blastAttack.procCoefficient;
            Speed = null;
        }

        public AttackInfo(BulletAttack bulletAttack, Vector3 normal, int muzzleIndex)
        {
            Vector3 position = bulletAttack.origin;
            Vector3 muzzlePosition = position;

            if (bulletAttack.weapon && bulletAttack.weapon.TryGetComponent(out ModelLocator weaponModelLocator))
            {
                Transform weaponModelTransform = weaponModelLocator.modelTransform;
                if (weaponModelTransform && weaponModelTransform.TryGetComponent(out ChildLocator weaponChildLocator))
                {
                    Transform muzzleTransform = weaponChildLocator.FindChild(muzzleIndex);
                    if (muzzleTransform)
                    {
                        muzzlePosition = muzzleTransform.position;
                    }
                }
            }

            Attacker = bulletAttack.owner;
            Inflictor = bulletAttack.weapon;
            Target = null;
            Position = position;
            MuzzlePosition = muzzlePosition;
            AttackDirection = normal;
            Damage = bulletAttack.damage;
            Force = bulletAttack.force;
            Crit = bulletAttack.isCrit;
            DamageColorIndex = bulletAttack.damageColorIndex;
            ProcChainMask = bulletAttack.procChainMask;
            DamageTypeOverride = bulletAttack.damageType;
            ProcCoefficient = bulletAttack.procCoefficient;
            Speed = null;
        }

        public AttackInfo(Orb orb)
        {
            GameObject owner = null;
            GameObject target = null;
            Vector3 position = orb.origin;
            Vector3 direction = Vector3.zero;
            float damage = 0f;
            float force = 0f;
            bool isCrit = false;
            DamageColorIndex damageColorIndex = DamageColorIndex.Default;
            ProcChainMask procChainMask = default;
            DamageTypeCombo damageType = DamageTypeCombo.Generic;
            float? speed = null;
            float procCoefficient = 0f;

            if (orb.target)
            {
                Vector3 targetPosition = orb.target.transform.position;
                if (position == Vector3.zero)
                {
                    position = targetPosition;
                }

                Vector3 moveVector = targetPosition - position;
                if (moveVector.sqrMagnitude > 0f)
                {
                    direction = moveVector.normalized;
                }

                target = orb.target.gameObject;

                if (orb.distanceToTarget > 0f && orb.duration > 0f)
                {
                    speed = orb.distanceToTarget / orb.duration;
                }
            }

            CharacterBody orbAttacker = orb.GetAttacker();
            if (orbAttacker)
            {
                owner = orbAttacker.gameObject;
            }

            if (orb.TryGetDamageValue(out float orbDamage))
            {
                damage = Mathf.Max(0f, orbDamage);
            }

            if (orb.TryGetForceScalar(out float orbForceScalar))
            {
                force = Mathf.Max(0f, orbForceScalar);
            }

            if (orb.TryGetIsCrit(out bool orbIsCrit))
            {
                isCrit = orbIsCrit;
            }

            if (orb.TryGetDamageColorIndex(out DamageColorIndex orbDamageColorIndex))
            {
                damageColorIndex = orbDamageColorIndex;
            }

            if (orb.TryGetProcChainMask(out ProcChainMask orbProcChainMask))
            {
                procChainMask = orbProcChainMask;
            }

            if (orb.TryGetDamageType(out DamageTypeCombo orbDamageType))
            {
                damageType = orbDamageType;
            }

            if (orb.TryGetProcCoefficient(out float orbProcCoefficient))
            {
                procCoefficient = orbProcCoefficient;
            }

            Attacker = owner;
            Target = target;
            Position = position;
            MuzzlePosition = position;
            AttackDirection = direction;
            Damage = damage;
            Force = force;
            Crit = isCrit;
            DamageColorIndex = damageColorIndex;
            ProcChainMask = procChainMask;
            DamageTypeOverride = damageType;
            ProcCoefficient = procCoefficient;
            Speed = speed;
        }

        public AttackInfo(OverlapAttack overlapAttack)
        {
            Vector3 position = Vector3.zero;
            Vector3 direction = Vector3.zero;
            if (overlapAttack.hitBoxGroup)
            {
                Transform hitBoxGroupTransform = overlapAttack.hitBoxGroup.transform;
                position = hitBoxGroupTransform.position;
                direction = hitBoxGroupTransform.forward;

                HitBox[] hitBoxes = overlapAttack.hitBoxGroup.hitBoxes;
                if (hitBoxes != null && hitBoxes.Length > 0)
                {
                    position = Vector3.zero;

                    foreach (HitBox hitBox in hitBoxes)
                    {
                        position += hitBox.transform.position;
                    }

                    position /= hitBoxes.Length;
                }
            }

            Attacker = overlapAttack.attacker;
            Inflictor = overlapAttack.inflictor;
            Target = null;
            Position = position;
            MuzzlePosition = position;
            AttackDirection = direction;
            Damage = overlapAttack.damage;
            Force = overlapAttack.pushAwayForce;
            Crit = overlapAttack.isCrit;
            DamageColorIndex = overlapAttack.damageColorIndex;
            ProcChainMask = overlapAttack.procChainMask;
            DamageTypeOverride = overlapAttack.damageType;
            ProcCoefficient = overlapAttack.procCoefficient;
            Speed = null;
        }

        public AttackInfo(EntityStates.GolemMonster.FireLaser fireLaserState)
        {
            Vector3 position = fireLaserState.GetAimRay().origin;
            Vector3 muzzlePosition = position;

            Transform laserMuzzleTransform = fireLaserState.FindModelChild("MuzzleLaser");
            if (laserMuzzleTransform)
            {
                muzzlePosition = laserMuzzleTransform.position;
            }

            Attacker = fireLaserState.gameObject;
            Target = null;
            Position = position;
            MuzzlePosition = muzzlePosition;
            AttackDirection = fireLaserState.laserDirection;
            Damage = EntityStates.GolemMonster.FireLaser.damageCoefficient * fireLaserState.damageStat;
            Force = EntityStates.GolemMonster.FireLaser.force;
            Crit = false;
            DamageColorIndex = DamageColorIndex.Default;
            ProcChainMask = default;
            DamageTypeOverride = DamageTypeCombo.Generic;
            ProcCoefficient = 0f;
            Speed = null;
        }

        public AttackInfo(EntityStates.Merc.Evis evis, HurtBox target)
        {
            Vector3 position = target.transform.position;

            Attacker = evis.gameObject;
            Target = target.gameObject;
            Position = position;
            MuzzlePosition = position;
            AttackDirection = Vector3.zero;
            Damage = EntityStates.Merc.Evis.damageCoefficient * evis.damageStat;
            Force = 0f;
            Crit = evis.crit;
            DamageColorIndex = DamageColorIndex.Default;
            ProcChainMask = default;
            DamageTypeOverride = DamageTypeCombo.GenericSpecial;
            ProcCoefficient = EntityStates.Merc.Evis.procCoefficient;
        }

        public readonly void PopulateFireProjectileInfo(ref FireProjectileInfo fireProjectileInfo)
        {
            fireProjectileInfo.position = MuzzlePosition;
            fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(AttackDirection);
            fireProjectileInfo.owner = Attacker;
            fireProjectileInfo.target = Target;
            fireProjectileInfo.damage = Damage;
            fireProjectileInfo.force = Force;
            fireProjectileInfo.crit = Crit;
            fireProjectileInfo.damageColorIndex = DamageColorIndex;
            fireProjectileInfo.procChainMask = ProcChainMask;
            fireProjectileInfo.damageTypeOverride = DamageTypeOverride;

            if (Speed.HasValue)
            {
                fireProjectileInfo.speedOverride = Speed.Value;
            }

            fireProjectileInfo.SetProcCoefficientOverride(ProcCoefficient);
        }

        public readonly void PopulateBulletAttack(BulletAttack bulletAttack)
        {
            bulletAttack.origin = MuzzlePosition;
            bulletAttack.aimVector = AttackDirection;
            bulletAttack.owner = Attacker;
            bulletAttack.weapon = Inflictor ?? Attacker;
            bulletAttack.damage = Damage;
            bulletAttack.force = Force;
            bulletAttack.isCrit = Crit;
            bulletAttack.damageColorIndex = DamageColorIndex;
            bulletAttack.procChainMask = ProcChainMask;
            bulletAttack.damageType = DamageTypeOverride.GetValueOrDefault(DamageTypeCombo.Generic);
            bulletAttack.procCoefficient = ProcCoefficient;
        }

        public readonly void PopulateBlastAttack(BlastAttack blastAttack)
        {
            blastAttack.attacker = Attacker;
            blastAttack.inflictor = Inflictor ?? Attacker;
            blastAttack.teamIndex = TeamComponent.GetObjectTeam(Attacker);
            blastAttack.position = MuzzlePosition;
            blastAttack.baseDamage = Damage;
            blastAttack.baseForce = Force;
            blastAttack.bonusForce = AttackDirection * Force;
            blastAttack.crit = Crit;
            blastAttack.damageType = DamageTypeOverride.GetValueOrDefault(DamageTypeCombo.Generic);
            blastAttack.damageColorIndex = DamageColorIndex;
            blastAttack.procChainMask = ProcChainMask;
            blastAttack.procCoefficient = ProcCoefficient;
        }

        public readonly void PopulateOverlapAttack(OverlapAttack overlapAttack)
        {
            overlapAttack.attacker = Attacker;
            overlapAttack.inflictor = Inflictor ?? Attacker;
            overlapAttack.teamIndex = TeamComponent.GetObjectTeam(Attacker);
            overlapAttack.forceVector = AttackDirection * Force;
            overlapAttack.pushAwayForce = Force;
            overlapAttack.damage = Damage;
            overlapAttack.isCrit = Crit;
            overlapAttack.procChainMask = ProcChainMask;
            overlapAttack.procCoefficient = ProcCoefficient;
            overlapAttack.damageColorIndex = DamageColorIndex;
            overlapAttack.damageType = DamageTypeOverride.GetValueOrDefault(DamageTypeCombo.Generic);
        }

        public readonly void PopulateDamageInfo(DamageInfo damageInfo)
        {
            damageInfo.damage = Damage;
            damageInfo.crit = Crit;
            damageInfo.inflictor = Inflictor ?? Attacker;
            damageInfo.attacker = Attacker;
            damageInfo.position = MuzzlePosition;
            damageInfo.force = AttackDirection * Force;
            damageInfo.procChainMask = ProcChainMask;
            damageInfo.procCoefficient = ProcCoefficient;
            damageInfo.damageType = DamageTypeOverride.GetValueOrDefault(DamageTypeCombo.Generic);
            damageInfo.damageColorIndex = DamageColorIndex;
        }

        public readonly void PopulateOrb(Orb orb)
        {
            orb.origin = MuzzlePosition;

            HurtBox targetHurtBox = null;
            if (Target)
            {
                targetHurtBox = Target.GetComponent<HurtBox>();
            }

            orb.target = targetHurtBox;

            if (Speed.HasValue && Speed.Value > 0f)
            {
                orb.duration = orb.distanceToTarget / Speed.Value;
            }

            CharacterBody attackerBody = null;
            if (Attacker)
            {
                attackerBody = Attacker.GetComponent<CharacterBody>();
            }

            orb.TrySetAttacker(attackerBody);

            orb.TrySetDamageValue(Damage);

            orb.TrySetForceScalar(Force);

            orb.TrySetIsCrit(Crit);

            orb.TrySetDamageColorIndex(DamageColorIndex);

            orb.TrySetProcChainMask(ProcChainMask);

            if (DamageTypeOverride.HasValue)
            {
                orb.TrySetDamageType(DamageTypeOverride.Value);
            }

            orb.TrySetProcCoefficient(ProcCoefficient);
        }
    }
}
