using RiskOfChaos.Utilities.Interpolation;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Projectile
{
    [ValueModificationManager]
    public class ProjectileModificationManager : NetworkedValueModificationManager<ProjectileModificationData>
    {
        static ProjectileModificationManager _instance;
        public static ProjectileModificationManager Instance => _instance;

        const uint TOTAL_PROJECTILE_SPEED_MULTIPLIER_DIRTY_BIT = 1 << 1;

        float _totalProjectileSpeedMultiplier = 1f;
        public float NetworkedTotalProjectileSpeedMultiplier
        {
            get
            {
                return _totalProjectileSpeedMultiplier;
            }

            [param: In]
            set
            {
                SetSyncVar(value, ref _totalProjectileSpeedMultiplier, TOTAL_PROJECTILE_SPEED_MULTIPLIER_DIRTY_BIT);
            }
        }

        const uint PROJECTILE_BOUNCE_COUNT_DIRTY_BIT = 1 << 2;

        uint _projectileBounceCount;
        public uint NetworkedProjectileBounceCount
        {
            get
            {
                return _projectileBounceCount;
            }

            [param: In]
            set
            {
                SetSyncVar(value, ref _projectileBounceCount, PROJECTILE_BOUNCE_COUNT_DIRTY_BIT);
            }
        }

        const uint BULLET_BOUNCE_COUNT_DIRTY_BIT = 1 << 3;

        uint _bulletBounceCount;
        public uint NetworkedBulletBounceCount
        {
            get
            {
                return _bulletBounceCount;
            }

            [param: In]
            set
            {
                SetSyncVar(value, ref _bulletBounceCount, BULLET_BOUNCE_COUNT_DIRTY_BIT);
            }
        }

        const uint ORB_BOUNCE_COUNT_DIRTY_BIT = 1 << 4;

        uint _orbBounceCount;
        public uint NetworkedOrbBounceCount
        {
            get
            {
                return _orbBounceCount;
            }

            [param: In]
            set
            {
                SetSyncVar(value, ref _orbBounceCount, ORB_BOUNCE_COUNT_DIRTY_BIT);
            }
        }

        const uint EXTRA_SPAWN_COUNT_DIRTY_BIT = 1 << 5;

        byte _extraSpawnCount;
        public byte NetworkedExtraSpawnCount
        {
            get
            {
                return _extraSpawnCount;
            }
            set
            {
                SetSyncVar(value, ref _extraSpawnCount, EXTRA_SPAWN_COUNT_DIRTY_BIT);
            }
        }

        public override ProjectileModificationData InterpolateValue(in ProjectileModificationData a, in ProjectileModificationData b, float t)
        {
            return ProjectileModificationData.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            ProjectileModificationData modificationData = GetModifiedValue(new ProjectileModificationData());
            NetworkedTotalProjectileSpeedMultiplier = modificationData.SpeedMultiplier;

            NetworkedProjectileBounceCount = modificationData.ProjectileBounceCount;
            NetworkedBulletBounceCount = modificationData.BulletBounceCount;
            NetworkedOrbBounceCount = modificationData.OrbBounceCount;

            NetworkedExtraSpawnCount = modificationData.ExtraSpawnCount;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SingletonHelper.Assign(ref _instance, this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SingletonHelper.Unassign(ref _instance, this);
        }

        protected override bool serialize(NetworkWriter writer, bool initialState, uint dirtyBits)
        {
            bool baseResult = base.serialize(writer, initialState, dirtyBits);
            if (initialState)
            {
                writer.Write(_totalProjectileSpeedMultiplier);
                writer.WritePackedUInt32(_projectileBounceCount);
                writer.WritePackedUInt32(_bulletBounceCount);
                writer.WritePackedUInt32(_orbBounceCount);
                writer.Write(_extraSpawnCount);
                return true;
            }

            bool anythingWritten = false;

            if ((dirtyBits & TOTAL_PROJECTILE_SPEED_MULTIPLIER_DIRTY_BIT) != 0)
            {
                writer.Write(_totalProjectileSpeedMultiplier);
                anythingWritten = true;
            }

            if ((dirtyBits & PROJECTILE_BOUNCE_COUNT_DIRTY_BIT) != 0)
            {
                writer.WritePackedUInt32(_projectileBounceCount);
                anythingWritten = true;
            }

            if ((dirtyBits & BULLET_BOUNCE_COUNT_DIRTY_BIT) != 0)
            {
                writer.WritePackedUInt32(_bulletBounceCount);
                anythingWritten = true;
            }

            if ((dirtyBits & ORB_BOUNCE_COUNT_DIRTY_BIT) != 0)
            {
                writer.WritePackedUInt32(_orbBounceCount);
                anythingWritten = true;
            }

            if ((dirtyBits & EXTRA_SPAWN_COUNT_DIRTY_BIT) != 0)
            {
                writer.Write(_extraSpawnCount);
                anythingWritten = true;
            }

            return baseResult || anythingWritten;
        }

        protected override void deserialize(NetworkReader reader, bool initialState, uint dirtyBits)
        {
            base.deserialize(reader, initialState, dirtyBits);

            if (initialState)
            {
                _totalProjectileSpeedMultiplier = reader.ReadSingle();
                _projectileBounceCount = reader.ReadPackedUInt32();
                _bulletBounceCount = reader.ReadPackedUInt32();
                _orbBounceCount = reader.ReadPackedUInt32();
                _extraSpawnCount = reader.ReadByte();
                return;
            }

            if ((dirtyBits & TOTAL_PROJECTILE_SPEED_MULTIPLIER_DIRTY_BIT) != 0)
            {
                _totalProjectileSpeedMultiplier = reader.ReadSingle();
            }

            if ((dirtyBits & PROJECTILE_BOUNCE_COUNT_DIRTY_BIT) != 0)
            {
                _projectileBounceCount = reader.ReadPackedUInt32();
            }

            if ((dirtyBits & BULLET_BOUNCE_COUNT_DIRTY_BIT) != 0)
            {
                _bulletBounceCount = reader.ReadPackedUInt32();
            }

            if ((dirtyBits & ORB_BOUNCE_COUNT_DIRTY_BIT) != 0)
            {
                _orbBounceCount = reader.ReadPackedUInt32();
            }

            if ((dirtyBits & EXTRA_SPAWN_COUNT_DIRTY_BIT) != 0)
            {
                _extraSpawnCount = reader.ReadByte();
            }
        }
    }
}
