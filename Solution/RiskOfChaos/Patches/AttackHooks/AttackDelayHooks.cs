using RiskOfChaos.ModificationController.AttackDelay;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
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

        public static bool TryDelayAttack(Action spawnFunc, AttackHookMask activeAttackHooks)
        {
            if (spawnFunc == null)
                return false;

            if (_delayedAttackTimers == null)
                return false;

            float delay = totalDelay;
            if (delay <= 0f)
                return false;

            _delayedAttackTimers.CreateTimer(delay, () =>
            {
                AttackHookManager.Context.Activate(activeAttackHooks | AttackHookMask.Delayed);
                spawnFunc();
            });

            return true;
        }
    }
}
