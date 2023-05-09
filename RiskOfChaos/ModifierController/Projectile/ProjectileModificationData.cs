namespace RiskOfChaos.ModifierController.Projectile
{
    public struct ProjectileModificationData
    {
        public float SpeedMultiplier = 1f;

        public ProjectileBounceFlags BounceFlags;
        public uint ProjectileBounceCount;
        public uint BulletBounceCount;

        public ProjectileModificationData()
        {
        }
    }
}
