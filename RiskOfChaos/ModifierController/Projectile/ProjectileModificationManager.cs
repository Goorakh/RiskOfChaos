using RiskOfChaos.Utilities.Interpolation;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Projectile
{
    [ValueModificationManager(typeof(SyncProjectileModification))]
    public class ProjectileModificationManager : ValueModificationManager<ProjectileModificationData>
    {
        static ProjectileModificationManager _instance;
        public static ProjectileModificationManager Instance => _instance;

        SyncProjectileModification _clientSync;

        public override bool AnyModificationActive => NetworkServer.active ? base.AnyModificationActive : _clientSync.AnyModificationActive;

        public float TotalProjectileSpeedMultiplier
        {
            get
            {
                return _clientSync.SpeedMultiplier;
            }
            private set
            {
                _clientSync.SpeedMultiplier = value;
            }
        }

        public uint ProjectileBounceCount
        {
            get
            {
                return _clientSync.ProjectileBounceCount;
            }
            private set
            {
                _clientSync.ProjectileBounceCount = value;
            }
        }

        public uint BulletBounceCount
        {
            get
            {
                return _clientSync.BulletBounceCount;
            }
            private set
            {
                _clientSync.BulletBounceCount = value;
            }
        }

        public uint OrbBounceCount
        {
            get
            {
                return _clientSync.OrbBounceCount;
            }
            private set
            {
                _clientSync.OrbBounceCount = value;
            }
        }

        public byte ExtraSpawnCount
        {
            get
            {
                return _clientSync.ExtraSpawnCount;
            }
            private set
            {
                _clientSync.ExtraSpawnCount = value;
            }
        }

        void Awake()
        {
            _clientSync = GetComponent<SyncProjectileModification>();
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

        public override ProjectileModificationData InterpolateValue(in ProjectileModificationData a, in ProjectileModificationData b, float t)
        {
            return ProjectileModificationData.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            _clientSync.AnyModificationActive = base.AnyModificationActive;

            ProjectileModificationData modificationData = GetModifiedValue(new ProjectileModificationData());
            TotalProjectileSpeedMultiplier = modificationData.SpeedMultiplier;

            ProjectileBounceCount = modificationData.ProjectileBounceCount;
            BulletBounceCount = modificationData.BulletBounceCount;
            OrbBounceCount = modificationData.OrbBounceCount;

            ExtraSpawnCount = modificationData.ExtraSpawnCount;
        }
    }
}
