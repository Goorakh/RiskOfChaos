using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.ModificationController.Projectile
{
    public sealed class ProjectileModificationManager : MonoBehaviour
    {
        static ProjectileModificationManager _instance;
        public static ProjectileModificationManager Instance => _instance;

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkPrefabs)
        {
            // ProjectileModificationProvider
            {
                GameObject prefab = Prefabs.CreateValueModificationProviderPrefab(typeof(ProjectileModificationProvider), nameof(RoCContent.NetworkedPrefabs.ProjectileModificationProvider), false);

                networkPrefabs.Add(prefab);
            }
        }

        ValueModificationProviderHandler<ProjectileModificationProvider> _modificationProviderHandler;

        public bool AnyModificationActive { get; private set; }

        public float SpeedMultiplier { get; private set; }

        public int ProjectileBounceCount { get; private set; }

        public int BulletBounceCount { get; private set; }

        public int OrbBounceCount { get; private set; }

        public int AdditionalSpawnCount { get; private set; }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _modificationProviderHandler = new ValueModificationProviderHandler<ProjectileModificationProvider>(refreshValueModifications);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            if (_modificationProviderHandler != null)
            {
                _modificationProviderHandler.Dispose();
                _modificationProviderHandler = null;
            }
        }

        void refreshValueModifications(IReadOnlyCollection<ProjectileModificationProvider> modificationProviders)
        {
            bool anyModificationActive = false;
            float speedMultiplier = 1f;
            int projectileBounceCount = 0;
            int bulletBounceCount = 0;
            int orbBounceCount = 0;
            int additionalSpawnCount = 0;

            foreach (ProjectileModificationProvider modificationProvider in modificationProviders)
            {
                anyModificationActive = true;
                speedMultiplier *= modificationProvider.SpeedMultiplier;
                projectileBounceCount += modificationProvider.ProjectileBounceCount;
                bulletBounceCount += modificationProvider.BulletBounceCount;
                orbBounceCount += modificationProvider.OrbBounceCount;
                additionalSpawnCount += modificationProvider.AdditionalSpawnCount;
            }

            AnyModificationActive = anyModificationActive;
            SpeedMultiplier = Mathf.Max(0f, speedMultiplier);
            ProjectileBounceCount = Mathf.Max(0, projectileBounceCount);
            BulletBounceCount = Mathf.Max(0, bulletBounceCount);
            OrbBounceCount = Mathf.Max(0, orbBounceCount);
            AdditionalSpawnCount = Mathf.Clamp(additionalSpawnCount, 0, 255);
        }
    }
}
