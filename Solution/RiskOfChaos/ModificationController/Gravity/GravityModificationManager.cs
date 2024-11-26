using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
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
                GameObject prefab = Prefabs.CreateNetworkedValueModificationProviderPrefab(typeof(GravityModificationProvider), nameof(RoCContent.NetworkedPrefabs.GravityModificationProvider), true);

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
            float strengthMultiplier = 1f;
            Quaternion gravityRotation = Quaternion.identity;

            foreach (GravityModificationProvider modificationProvider in modificationProviders)
            {
                strengthMultiplier *= modificationProvider.GravityMultiplier;
                gravityRotation *= modificationProvider.GravityRotation;
            }

            strengthMultiplier = Mathf.Max(0.01f, strengthMultiplier);

            gravityRotation.ToAngleAxis(out float tiltAngle, out Vector3 tiltAxis);
            tiltAngle = Mathf.Min(tiltAngle, 89f);
            gravityRotation = Quaternion.AngleAxis(tiltAngle, tiltAxis);

            Vector3 gravity = GravityTracker.BaseGravity;

            Vector3 rotatedGravity = gravityRotation * gravity;

            const float BOOST_MIN_ANGLE = 0f;
            const float BOOST_MAX_ANGLE = 90f;
            const float BOOST_MIN = 1f;
            const float BOOST_MAX = 4f;

            float normalizedAngle = Mathf.InverseLerp(BOOST_MIN_ANGLE, BOOST_MAX_ANGLE, tiltAngle);

            float strengthBoostXZ = Mathf.Lerp(BOOST_MIN, BOOST_MAX, 1f - Ease.OutQuad(normalizedAngle));
            
            Vector3 gravityScale = new Vector3(strengthBoostXZ, 1f, strengthBoostXZ) * strengthMultiplier;

            Vector3 modifiedGravity = Vector3.Scale(rotatedGravity, gravityScale);

            GravityTracker.SetGravityUntracked(modifiedGravity);

            Log.Debug($"multiplier={strengthMultiplier}, tilt={tiltAngle}, XZ multiplier={strengthBoostXZ}. eff mult={modifiedGravity.magnitude / gravity.magnitude}, eff tilt={Vector3.Angle(gravity, modifiedGravity)}");
        }
    }
}
