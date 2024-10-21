using RiskOfChaos.Content;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.Projectile
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class ProjectileModificationProvider : NetworkBehaviour
    {
        ValueModificationController _modificationController;

        public ValueModificationConfigBinding<float> SpeedMultiplierConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setSpeedMultiplier))]
        public float SpeedMultiplier = 1f;

        public ValueModificationConfigBinding<int> ProjectileBounceCountConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setProjectiveBounceCount))]
        public int ProjectileBounceCount;

        public ValueModificationConfigBinding<int> BulletBounceCountConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setBulletBounceCount))]
        public int BulletBounceCount;

        public ValueModificationConfigBinding<int> OrbBounceCountConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setOrbBounceCount))]
        public int OrbBounceCount;

        public ValueModificationConfigBinding<int> AdditionalSpawnCountConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setAdditionalSpawnCount))]
        public int AdditionalSpawnCount;

        void Awake()
        {
            _modificationController = GetComponent<ValueModificationController>();
            _modificationController.OnRetire += onRetire;

            SpeedMultiplierConfigBinding = new ValueModificationConfigBinding<float>(v => SpeedMultiplier = v);
            ProjectileBounceCountConfigBinding = new ValueModificationConfigBinding<int>(v => ProjectileBounceCount = v);
            BulletBounceCountConfigBinding = new ValueModificationConfigBinding<int>(v => BulletBounceCount = v);
            OrbBounceCountConfigBinding = new ValueModificationConfigBinding<int>(v => OrbBounceCount = v);
            AdditionalSpawnCountConfigBinding = new ValueModificationConfigBinding<int>(v => AdditionalSpawnCount = v);
        }

        void OnDestroy()
        {
            _modificationController.OnRetire -= onRetire;
            disposeConfigBindings();
        }

        void onRetire()
        {
            disposeConfigBindings();
        }

        void disposeConfigBindings()
        {
            SpeedMultiplierConfigBinding?.Dispose();
            ProjectileBounceCountConfigBinding?.Dispose();
            BulletBounceCountConfigBinding?.Dispose();
            OrbBounceCountConfigBinding?.Dispose();
            AdditionalSpawnCountConfigBinding?.Dispose();
        }

        void onValueChanged()
        {
            if (_modificationController)
            {
                _modificationController.InvokeOnValuesDirty();
            }
        }

        void setSpeedMultiplier(float speedMultiplier)
        {
            SpeedMultiplier = speedMultiplier;
            onValueChanged();
        }

        void setProjectiveBounceCount(int projectiveBounceCount)
        {
            ProjectileBounceCount = projectiveBounceCount;
            onValueChanged();
        }

        void setBulletBounceCount(int bulletBounceCount)
        {
            BulletBounceCount = bulletBounceCount;
            onValueChanged();
        }

        void setOrbBounceCount(int orbBounceCount)
        {
            OrbBounceCount = orbBounceCount;
            onValueChanged();
        }

        void setAdditionalSpawnCount(int additionalSpawnCount)
        {
            AdditionalSpawnCount = additionalSpawnCount;
            onValueChanged();
        }
    }
}
