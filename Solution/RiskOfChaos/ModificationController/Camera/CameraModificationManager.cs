using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.ModificationController.Camera
{
    public sealed class CameraModificationManager : MonoBehaviour
    {
        static CameraModificationManager _instance;
        public static CameraModificationManager Instance => _instance;

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkPrefabs)
        {
            // CameraModificationProvider
            {
                GameObject prefab = Prefabs.CreateNetworkedValueModificationProviderPrefab(typeof(CameraModificationProvider), nameof(RoCContent.NetworkedPrefabs.CameraModificationProvider), true);

                networkPrefabs.Add(prefab);
            }
        }

        public bool AnyModificationActive { get; private set; }

        public Vector2 RecoilMultiplier { get; private set; }

        public float FOVMultiplier { get; private set; }

        public Quaternion RotationOffset { get; private set; }

        public float DistanceMultiplier { get; private set; }

        ValueModificationProviderHandler<CameraModificationProvider> _modificationProviderHandler;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _modificationProviderHandler = new ValueModificationProviderHandler<CameraModificationProvider>(refreshValueModifications, false);
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

        void Update()
        {
            _modificationProviderHandler?.Update();
        }

        void refreshValueModifications(IReadOnlyCollection<CameraModificationProvider> modificationProviders)
        {
            bool anyModificationActive = false;
            Vector2 recoilMultiplier = Vector2.one;
            float fovMultiplier = 1f;
            Quaternion rotationOffset = Quaternion.identity;
            float distanceMultiplier = 1f;

            foreach (CameraModificationProvider cameraModification in modificationProviders)
            {
                anyModificationActive = true;

                recoilMultiplier = Vector2.Scale(recoilMultiplier, cameraModification.RecoilMultiplier);
                fovMultiplier *= cameraModification.FOVMultiplier;
                rotationOffset *= cameraModification.RotationOffset;
                distanceMultiplier += cameraModification.DistanceMultiplier - 1f;
            }

            AnyModificationActive = anyModificationActive;
            RecoilMultiplier = recoilMultiplier;
            FOVMultiplier = Mathf.Max(0f, fovMultiplier);
            RotationOffset = rotationOffset;
            DistanceMultiplier = Mathf.Max(0f, distanceMultiplier);
        }
    }
}
