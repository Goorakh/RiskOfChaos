using RiskOfChaos.Content;
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
        static void LoadContent(ContentIntializerArgs args)
        {
            // EffectModificationProvider
            {
                GameObject prefab = Prefabs.CreateNetworkedValueModificationProviderPrefab(typeof(GravityModificationProvider), nameof(RoCContent.NetworkedPrefabs.GravityModificationProvider), true);

                args.ContentPack.networkedObjectPrefabs.Add([prefab]);
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

            // The "multiplier" that will be effectively introduced to the vertical component of the gravity
            float tiltDownStrengthMultiplier = Mathf.Cos(tiltAngle * Mathf.Deg2Rad);

            // "normalize" the vertical gravity to make down always have the same magnitude, regardless of tilt
            // Upper bound so gravity doesn't end up too insane, this bound cuts off at tilt=60°
            float tiltStrengthMultiplier = Mathf.Min(2f, 1f / tiltDownStrengthMultiplier);

            Vector3 gravity = GravityTracker.BaseGravity;

            Vector3 modifiedGravity = (gravityRotation * gravity) * (strengthMultiplier * tiltStrengthMultiplier);

            GravityTracker.SetGravityUntracked(modifiedGravity);

            Log.Debug($"multiplier={strengthMultiplier}, tilt={tiltAngle}. eff mult={modifiedGravity.magnitude / gravity.magnitude}, eff tilt={Vector3.Angle(gravity, modifiedGravity)}");
        }
    }
}
