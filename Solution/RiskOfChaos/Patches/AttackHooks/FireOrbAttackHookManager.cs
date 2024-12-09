using R2API;
using RiskOfChaos.Content;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos_PatcherInterop;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    class FireOrbAttackHookManager : AttackHookManager
    {
        readonly OrbManager _orbManager;
        readonly Orb _originalOrb;
        readonly Orb _orbTemplate;

        public FireOrbAttackHookManager(OrbManager orbManager, Orb orb)
        {
            _orbManager = orbManager;
            _originalOrb = orb;
            _orbTemplate = OrbUtils.Clone(orb);
        }

        OrbManager getOrbManager()
        {
            if (_orbManager)
                return _orbManager;

            return OrbManager.instance;
        }

        protected override void fireAttackCopy()
        {
            OrbManager orbManager = getOrbManager();
            if (!orbManager)
                return;

            orbManager.AddOrb(OrbUtils.Clone(_orbTemplate));
        }

        protected override bool tryFireRepeating(AttackHookMask activeAttackHooks)
        {
            if (OrbUtils.IsTransferOrb(_orbTemplate))
                return false;

            if (_orbTemplate.TryGetProcChainMask(out ProcChainMask procChainMask) && procChainMask.HasAnyProc())
                return false;

            if (_orbTemplate.TryGetBouncedObjects(out ReadOnlyCollection<HealthComponent> bouncedObjects) && bouncedObjects.Count > 0)
                return false;

            if (OrbBounceHook.IsBouncedOrb(_originalOrb))
                return false;

            return base.tryFireRepeating(activeAttackHooks);
        }

        protected override bool tryFireBounce(AttackHookMask activeAttackHooks)
        {
            if (OrbUtils.IsTransferOrb(_originalOrb) || _originalOrb is VoidLightningOrb)
                return false;

            if (_orbTemplate.TryGetProcChainMask(out ProcChainMask orbProcChain))
            {
                if (!orbProcChain.HasModdedProc(CustomProcTypes.Bouncing) && orbProcChain.HasAnyProc())
                {
                    return false;
                }
            }

            return OrbBounceHook.TryStartBounceOrb(_originalOrb, activeAttackHooks);
        }

        protected override bool setupProjectileFireInfo(ref FireProjectileInfo fireProjectileInfo)
        {
            if (OrbUtils.IsTransferOrb(_originalOrb))
                return false;

            Vector3 position = _orbTemplate.origin;
            Quaternion rotation = Quaternion.identity;
            GameObject owner = null;
            GameObject target = null;
            float? damage = null;
            float force = 0f;
            bool isCrit = false;
            DamageColorIndex damageColorIndex = DamageColorIndex.Default;
            ProcChainMask procChainMask = default;
            DamageTypeCombo damageType = DamageType.Generic;
            float? speedOverride = null;
            float procCoefficientOverride = 0f;

            if (_orbTemplate.target)
            {
                Vector3 targetPosition = _orbTemplate.target.transform.position;
                if (position == Vector3.zero)
                {
                    position = targetPosition;
                }

                rotation = Util.QuaternionSafeLookRotation(targetPosition - position);
                target = _orbTemplate.target.gameObject;

                if (_orbTemplate.distanceToTarget > 0f && _orbTemplate.duration > 0f)
                {
                    speedOverride = _orbTemplate.distanceToTarget / _orbTemplate.duration;
                }
            }

            CharacterBody orbAttacker = _orbTemplate.GetAttacker();
            if (orbAttacker)
            {
                owner = orbAttacker.gameObject;
            }

            if (_orbTemplate.TryGetDamageValue(out float orbDamage))
            {
                damage = Mathf.Max(0f, orbDamage);
            }

            if (_orbTemplate.TryGetForceScalar(out float orbForceScalar))
            {
                force = Mathf.Max(0f, orbForceScalar);
            }

            if (_orbTemplate.TryGetIsCrit(out bool orbIsCrit))
            {
                isCrit = orbIsCrit;
            }

            if (_orbTemplate.TryGetDamageColorIndex(out DamageColorIndex orbDamageColorIndex))
            {
                damageColorIndex = orbDamageColorIndex;
            }

            if (_orbTemplate.TryGetProcChainMask(out ProcChainMask orbProcChainMask))
            {
                procChainMask = orbProcChainMask;
            }

            if (_orbTemplate.TryGetDamageType(out DamageTypeCombo orbDamageType))
            {
                damageType = orbDamageType;
            }

            if (_orbTemplate.TryGetProcCoefficient(out float orbProcCoefficient))
            {
                procCoefficientOverride = orbProcCoefficient;
            }

            if (!damage.HasValue || damage.Value < 0f || position == Vector3.zero)
                return false;

            fireProjectileInfo.position = position;
            fireProjectileInfo.rotation = rotation;
            fireProjectileInfo.owner = owner;
            fireProjectileInfo.target = target;
            fireProjectileInfo.damage = damage.Value;
            fireProjectileInfo.force = force;
            fireProjectileInfo.crit = isCrit;
            fireProjectileInfo.damageColorIndex = damageColorIndex;
            fireProjectileInfo.procChainMask = procChainMask;
            fireProjectileInfo.damageTypeOverride = damageType;

            if (speedOverride.HasValue)
            {
                fireProjectileInfo.speedOverride = speedOverride.Value;
            }

            fireProjectileInfo.SetProcCoefficientOverride(procCoefficientOverride);

            return true;
        }
    }
}
