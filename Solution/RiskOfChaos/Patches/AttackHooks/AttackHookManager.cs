namespace RiskOfChaos.Patches.AttackHooks
{
    abstract class AttackHookManager
    {
        public static AttackContext Context;

        public virtual AttackHookMask RunHooks()
        {
            AttackHookMask activeAttackHooks = Context.Pop();
            AttackHookMask activatedAttackHooks = AttackHookMask.None;

            if ((activeAttackHooks & AttackHookMask.Delayed) == 0)
            {
                if (tryFireDelayed(activeAttackHooks))
                {
                    activatedAttackHooks |= AttackHookMask.Delayed;
                    return activatedAttackHooks;
                }
            }
            
            if ((activeAttackHooks & AttackHookMask.Bounced) == 0)
            {
                if ((activeAttackHooks & AttackHookMask.Repeat) == 0)
                {
                    if (tryFireRepeating(activeAttackHooks))
                    {
                        activatedAttackHooks |= AttackHookMask.Repeat;
                    }
                }

                if (tryFireBounce(activeAttackHooks))
                {
                    activatedAttackHooks |= AttackHookMask.Bounced;
                }
            }

            return activatedAttackHooks;
        }

        protected abstract void fireAttackCopy();

        protected virtual bool tryFireDelayed(AttackHookMask activeAttackHooks)
        {
            return AttackDelayHooks.TryDelayAttack(fireAttackCopy, activeAttackHooks);
        }

        protected virtual bool tryFireRepeating(AttackHookMask activeAttackHooks)
        {
            return AttackMultiSpawnHook.TryMultiSpawn(fireAttackCopy, activeAttackHooks);
        }

        protected virtual bool tryFireBounce(AttackHookMask activeAttackHooks)
        {
            return false;
        }
    }
}
