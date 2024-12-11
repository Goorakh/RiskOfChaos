using RiskOfChaos.Utilities;
using RoR2;
using RoR2BepInExPack.Utilities;
using System.Collections.Generic;

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

        protected override AttackInfo AttackInfo { get; }

        public OverlapAttackHookManager(OverlapAttack overlapAttack)
        {
            _overlapAttack = overlapAttack;
            _ignoredHealthComponentList = [.. _overlapAttack.ignoredHealthComponentList];
            _ignoredRemovalList = [.. _overlapAttack.ignoredRemovalList];

            AttackInfo = new AttackInfo(overlapAttack);
        }

        protected override AttackHookMask runHooksInternal(AttackHookMask activeAttackHooks)
        {
            if (_replacedOverlapAttacks.TryGetValue(_overlapAttack, out _))
                return AttackHookMask.Replaced;

            return base.runHooksInternal(activeAttackHooks);
        }

        OverlapAttack getAttackInstance(AttackHookMask activeAttackHooks)
        {
            OverlapAttack overlapAttack = _overlapAttack;

            if ((activeAttackHooks & AttackHookMask.Repeat) != 0)
            {
                overlapAttack = AttackUtils.Clone(overlapAttack);
                overlapAttack.ignoredHealthComponentList = [.. _ignoredHealthComponentList];
                overlapAttack.ignoredRemovalList = new Queue<(HealthComponent, float)>(_ignoredRemovalList);
            }

            return overlapAttack;
        }

        protected override void fireAttackCopy()
        {
            OverlapAttack overlapAttack = getAttackInstance(Context.Peek());
            overlapAttack.Fire();
        }

        protected override bool tryReplace(AttackHookMask activeAttackHooks)
        {
            OverlapAttack overlapAttack = getAttackInstance(activeAttackHooks);

            bool replaced = overlapAttack.hitBoxGroup && !_replacedOverlapAttacks.TryGetValue(overlapAttack, out _) && base.tryReplace(activeAttackHooks);
            if (replaced)
            {
                _replacedOverlapAttacks.Add(overlapAttack, new());
            }

            return replaced;
        }
    }
}
