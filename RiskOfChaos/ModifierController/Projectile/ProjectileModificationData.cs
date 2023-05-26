namespace RiskOfChaos.ModifierController.Projectile
{
    public struct ProjectileModificationData
    {
        public float SpeedMultiplier = 1f;

        public uint ProjectileBounceCount;
        public uint BulletBounceCount;
        public uint OrbBounceCount;

        public ProjectileModificationData()
        {
        }
    }
}
