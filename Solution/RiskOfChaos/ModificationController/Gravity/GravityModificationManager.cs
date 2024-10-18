using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Patches;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.Gravity
{
    public sealed class GravityModificationManager : MonoBehaviour
    {
        static GravityModificationManager _instance;
        public static GravityModificationManager Instance => _instance;

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkedPrefabs)
        {
            // EffectModificationProvider
            {
                GameObject prefab = Prefabs.CreateValueModificatinProviderPrefab(typeof(GravityModificationProvider), nameof(RoCContent.NetworkedPrefabs.GravityModificationProvider));

                networkedPrefabs.Add(prefab);
            }
        }

        ValueModificationProviderHandler<GravityModificationProvider> _modificationProviderHandler;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _modificationProviderHandler = new ValueModificationProviderHandler<GravityModificationProvider>(refreshGravityModifications);

            if (NetworkServer.active)
            {
                GravityTracker.OnBaseGravityChanged += onBaseGravityChanged;
            }
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            GravityTracker.OnBaseGravityChanged -= onBaseGravityChanged;

            if (_modificationProviderHandler != null)
            {
                _modificationProviderHandler.Dispose();
                _modificationProviderHandler = null;
            }
        }

        void onBaseGravityChanged(Vector3 newGravity)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_modificationProviderHandler != null && _modificationProviderHandler.ActiveProviders.Count > 0)
            {
                _modificationProviderHandler.MarkValueModificationsDirty();
            }
        }

        void refreshGravityModifications(IReadOnlyCollection<GravityModificationProvider> modificationProviders)
        {
            Vector3 gravity = GravityTracker.BaseGravity;

            foreach (GravityModificationProvider modificationProvider in modificationProviders)
            {
                gravity = modificationProvider.GravityRotation * (gravity * modificationProvider.GravityMultiplier);
            }

            GravityTracker.SetGravityUntracked(gravity);
        }
    }
}
