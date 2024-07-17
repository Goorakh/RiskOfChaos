using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Projectile
{
    public sealed class SyncProjectileModification : NetworkBehaviour
    {
        [SyncVar]
        public bool AnyModificationActive;

        [SyncVar]
        public float SpeedMultiplier = 1f;

        [SyncVar]
        public uint ProjectileBounceCount = 0;

        [SyncVar]
        public uint BulletBounceCount = 0;

        [SyncVar]
        public uint OrbBounceCount = 0;

        [SyncVar]
        public byte ExtraSpawnCount;
    }
}
