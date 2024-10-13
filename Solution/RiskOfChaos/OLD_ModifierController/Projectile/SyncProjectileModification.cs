using UnityEngine.Networking;

namespace RiskOfChaos.OLD_ModifierController.Projectile
{
    public sealed class SyncProjectileModification : NetworkBehaviour, IValueModificationFieldsProvider
    {
        [field: SyncVar]
        public bool AnyModificationActive { get; set; }

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
