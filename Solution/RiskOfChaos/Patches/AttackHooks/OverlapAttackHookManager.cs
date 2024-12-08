using RiskOfChaos.Utilities;
using RoR2;
using System.Collections.Generic;

namespace RiskOfChaos.Patches.AttackHooks
{
    class OverlapAttackHookManager : AttackHookManager
    {
        readonly OverlapAttack _overlapAttack;
        readonly HealthComponent[] _ignoredHealthComponentList;
        readonly (HealthComponent, float)[] _ignoredRemovalList;

        public OverlapAttackHookManager(OverlapAttack overlapAttack)
        {
            _overlapAttack = overlapAttack;
            _ignoredHealthComponentList = [.. _overlapAttack.ignoredHealthComponentList];
            _ignoredRemovalList = [.. _overlapAttack.ignoredRemovalList];
        }

        protected override void fireAttackCopy()
        {
            OverlapAttack overlapAttack = _overlapAttack;

            if ((Context.Peek() & AttackHookMask.Repeat) != 0)
            {
                overlapAttack = AttackUtils.Clone(overlapAttack);
                overlapAttack.ignoredHealthComponentList = [.. _ignoredHealthComponentList];
                overlapAttack.ignoredRemovalList = new Queue<(HealthComponent, float)>(_ignoredRemovalList);
            }

            overlapAttack.Fire();
        }
    }
}
