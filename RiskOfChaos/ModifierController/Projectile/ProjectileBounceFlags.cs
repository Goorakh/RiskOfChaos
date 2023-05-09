using System;

namespace RiskOfChaos.ModifierController.Projectile
{
    [Flags]
    public enum ProjectileBounceFlags : byte
    {
        None,
        Projectiles = 1 << 0,
        Bullets = 1 << 1,
        All = byte.MaxValue
    }
}
