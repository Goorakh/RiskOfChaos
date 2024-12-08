namespace RiskOfChaos.Patches.AttackHooks
{
    struct AttackContext
    {
        AttackHookMask _activeHookMask;

        public void Activate(AttackHookMask hookType)
        {
            _activeHookMask |= hookType;
        }

        public readonly AttackHookMask Peek()
        {
            return _activeHookMask;
        }

        public AttackHookMask Pop()
        {
            AttackHookMask state = _activeHookMask;
            _activeHookMask = AttackHookMask.None;
            return state;
        }
    }
}
