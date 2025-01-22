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
        readonly OverlapAttack _overlapAttackTemplate;
        readonly List<OverlapAttack.OverlapInfo> _hitList;

        protected override AttackInfo AttackInfo { get; }

        public OverlapAttackHookManager(OverlapAttack overlapAttack, List<OverlapAttack.OverlapInfo> hitList)
        {
            _overlapAttack = overlapAttack;
            _overlapAttackTemplate = AttackUtils.Clone(_overlapAttack);
            _hitList = new List<OverlapAttack.OverlapInfo>(hitList ?? []);

            AttackInfo = new AttackInfo(_overlapAttack);
        }

        protected override void fireAttackCopy(AttackInfo attackInfo)
        {
            OverlapAttack overlapAttack = AttackUtils.Clone(_overlapAttackTemplate);
            attackInfo.PopulateOverlapAttack(overlapAttack);
            overlapAttack.ProcessHits(_hitList);
        }

        protected override bool tryFireDelayed()
        {
            return _hitList.Count > 0 && base.tryFireDelayed();
        }

        protected override bool tryFireRepeating()
        {
            return _hitList.Count > 0 && base.tryFireRepeating();
        }

        protected override bool tryReplace()
        {
            return _hitList.Count > 0 && base.tryReplace();
        }

        protected override bool tryKnockback()
        {
            return _hitList.Count > 0 && base.tryKnockback();
        }
    }
}
