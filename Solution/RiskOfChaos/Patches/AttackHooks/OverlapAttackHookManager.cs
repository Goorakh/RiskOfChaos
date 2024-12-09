using RiskOfChaos.Utilities;
using RiskOfChaos_PatcherInterop;
using RoR2;
using RoR2.Projectile;
using RoR2BepInExPack.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    class OverlapAttackHookManager : AttackHookManager
    {
        class DummyClass { }
        static readonly FixedConditionalWeakTable<OverlapAttack, DummyClass> _replacedOverlapAttacks = new FixedConditionalWeakTable<OverlapAttack, DummyClass>();

        static OverlapAttackHookManager()
        {
            OverlapAttackHooks.OnOverlapAttackResetIgnoredHealthComponents += onOverlapAttackResetIgnoredHealthComponents;
        }

        static void onOverlapAttackResetIgnoredHealthComponents(OverlapAttack overlapAttack)
        {
            _replacedOverlapAttacks.Remove(overlapAttack);
        }

        readonly OverlapAttack _overlapAttack;
        readonly HealthComponent[] _ignoredHealthComponentList;
        readonly (HealthComponent, float)[] _ignoredRemovalList;

        public OverlapAttackHookManager(OverlapAttack overlapAttack)
        {
            _overlapAttack = overlapAttack;
            _ignoredHealthComponentList = [.. _overlapAttack.ignoredHealthComponentList];
            _ignoredRemovalList = [.. _overlapAttack.ignoredRemovalList];
        }

        protected override AttackHookMask runHooksInternal(AttackHookMask activeAttackHooks)
        {
            if (_replacedOverlapAttacks.TryGetValue(_overlapAttack, out _))
                return AttackHookMask.Replaced;

            return base.runHooksInternal(activeAttackHooks);
        }

        OverlapAttack getAttackInstance()
        {
            OverlapAttack overlapAttack = _overlapAttack;

            if ((Context.Peek() & AttackHookMask.Repeat) != 0)
            {
                overlapAttack = AttackUtils.Clone(overlapAttack);
                overlapAttack.ignoredHealthComponentList = [.. _ignoredHealthComponentList];
                overlapAttack.ignoredRemovalList = new Queue<(HealthComponent, float)>(_ignoredRemovalList);
            }

            return overlapAttack;
        }

        protected override void fireAttackCopy()
        {
            OverlapAttack overlapAttack = getAttackInstance();
            overlapAttack.Fire();
        }

        protected override bool setupProjectileFireInfo(ref FireProjectileInfo fireProjectileInfo)
        {
            OverlapAttack overlapAttack = getAttackInstance();
            if (!overlapAttack.hitBoxGroup || _replacedOverlapAttacks.TryGetValue(overlapAttack, out _))
                return false;

            Transform hitBoxGroupTransform = overlapAttack.hitBoxGroup.transform;
            Vector3 position = hitBoxGroupTransform.position;
            Vector3 direction = hitBoxGroupTransform.forward;

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

            fireProjectileInfo.position = position;
            fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(direction);
            fireProjectileInfo.owner = overlapAttack.attacker;
            fireProjectileInfo.damage = overlapAttack.damage;
            fireProjectileInfo.force = overlapAttack.pushAwayForce;
            fireProjectileInfo.crit = overlapAttack.isCrit;
            fireProjectileInfo.damageColorIndex = overlapAttack.damageColorIndex;
            fireProjectileInfo.procChainMask = overlapAttack.procChainMask;
            fireProjectileInfo.damageTypeOverride = overlapAttack.damageType;
            fireProjectileInfo.SetProcCoefficientOverride(overlapAttack.procCoefficient);

            _replacedOverlapAttacks.Add(overlapAttack, new());

            return true;
        }
    }
}
