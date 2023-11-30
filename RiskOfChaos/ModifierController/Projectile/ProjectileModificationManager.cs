using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Projectile
{
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

        protected override ProjectileModificationData interpolateValue(in ProjectileModificationData a, in ProjectileModificationData b, float t, ValueInterpolationFunctionType interpolationType)
        {
            return ProjectileModificationData.Interpolate(a, b, t, interpolationType);
        }

        protected override void updateValueModifications()
        {
            ProjectileModificationData modificationData = getModifiedValue(new ProjectileModificationData());
            NetworkedTotalProjectileSpeedMultiplier = modificationData.SpeedMultiplier;

            NetworkedProjectileBounceCount = modificationData.ProjectileBounceCount;
            NetworkedBulletBounceCount = modificationData.BulletBounceCount;
            NetworkedOrbBounceCount = modificationData.OrbBounceCount;
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
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
        }
    }
}
