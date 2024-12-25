using R2API;
using RiskOfChaos.Content;
using RiskOfChaos.Utilities;
using RoR2;
using System.Collections.Generic;

namespace RiskOfChaos.Patches.AttackHooks
{
    class OverlapAttackHookManager : AttackHookManager
    {
        static OverlapAttackHookManager()
        {
            OverlapAttackHooks.OnOverlapAttackResetIgnoredHealthComponents += onOverlapAttackResetIgnoredHealthComponents;
        }

        static void onOverlapAttackResetIgnoredHealthComponents(OverlapAttack overlapAttack)
        {
            for (ModdedProcType moddedProcType = 0; moddedProcType < (ModdedProcType)ProcTypeAPI.ModdedProcTypeCount; moddedProcType++)
            {
                if (CustomProcTypes.IsMarkerProc(moddedProcType))
                {
                    overlapAttack.procChainMask.RemoveModdedProc(moddedProcType);
                }
            }
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

        OverlapAttack getAttackInstance(AttackInfo attackInfo)
        {
            OverlapAttack overlapAttack = _overlapAttack;

            if (attackInfo.ProcChainMask.HasModdedProc(CustomProcTypes.Repeated))
            {
                overlapAttack = AttackUtils.Clone(overlapAttack);
                overlapAttack.ignoredHealthComponentList = [.. _ignoredHealthComponentList];
                overlapAttack.ignoredRemovalList = new Queue<(HealthComponent, float)>(_ignoredRemovalList);
            }

            return overlapAttack;
        }

        protected override void fireAttackCopy(AttackInfo attackInfo)
        {
            OverlapAttack overlapAttack = getAttackInstance(attackInfo);
            attackInfo.PopulateOverlapAttack(overlapAttack);
            overlapAttack.Fire();
        }

        protected override bool tryFireDelayed()
        {
            bool delayed = base.tryFireDelayed();

            if (delayed)
            {
                _overlapAttack.procChainMask.AddModdedProc(CustomProcTypes.Delayed);
            }

            return delayed;
        }

        protected override bool tryFireRepeating()
        {
            bool repeated = base.tryFireRepeating();

            if (repeated)
            {
                _overlapAttack.procChainMask.AddModdedProc(CustomProcTypes.Repeated);
            }

            return repeated;
        }

        protected override bool tryReplace()
        {
            bool replaced = base.tryReplace();

            if (replaced)
            {
                _overlapAttack.procChainMask.AddModdedProc(CustomProcTypes.Replaced);
            }

            return replaced;
        }
    }
}
