using R2API;
using RiskOfChaos.Content;
using RiskOfChaos.ModificationController.AttackDelay;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    static class AttackDelayHooks
    {
        static readonly TimerQueue _delayedAttackTimers = new TimerQueue();

        static float totalDelay
        {
            get
            {
                AttackDelayModificationManager instance = AttackDelayModificationManager.Instance;
                if (!instance || !instance.AnyModificationActive)
                    return 0f;

                return instance.TotalDelay;
            }
        }

        [SystemInitializer]
        static void Init()
        {
            Run.onRunStartGlobal += _ =>
            {
                _delayedAttackTimers.Clear();
                RoR2Application.onUpdate += update;
            };

            Run.onRunDestroyGlobal += _ =>
            {
                RoR2Application.onUpdate -= update;
                _delayedAttackTimers.Clear();
            };

            Stage.onServerStageComplete += _ =>
            {
                _delayedAttackTimers.Clear();
            };
        }

        static void update()
        {
            _delayedAttackTimers.Update(Time.deltaTime);
        }

        public static bool TryDelayAttack(AttackHookManager.FireAttackDelegate spawnFunc, in AttackInfo attackInfo)
        {
            if (spawnFunc == null)
                return false;

            if (_delayedAttackTimers == null)
                return false;

            float delay = totalDelay;
            if (delay <= 0f)
                return false;

            AttackInfo delayedAttackInfo = attackInfo;
            delayedAttackInfo.ProcChainMask.AddModdedProc(CustomProcTypes.Delayed);

            _delayedAttackTimers.CreateTimer(delay, () =>
            {
                spawnFunc(delayedAttackInfo);
            });

            return true;
        }
    }
}
