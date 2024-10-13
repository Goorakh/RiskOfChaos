using RiskOfChaos.Networking.Components.Gravity;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities.Interpolation;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.OLD_ModifierController.Gravity
{
    [ValueModificationManager(typeof(GravitySync), typeof(SyncGravityModification))]
    public class GravityModificationManager : ValueModificationManager<Vector3>
    {
        static GravityModificationManager _instance;
        public static GravityModificationManager Instance => _instance;

        SyncGravityModification _clientSync;

        protected override void Awake()
        {
            base.Awake();
            _clientSync = GetComponent<SyncGravityModification>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SingletonHelper.Assign(ref _instance, this);

            if (NetworkServer.active)
            {
                GravityTracker.OnBaseGravityChanged += onBaseGravityChanged;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SingletonHelper.Unassign(ref _instance, this);

            GravityTracker.OnBaseGravityChanged -= onBaseGravityChanged;
        }

        void onBaseGravityChanged(Vector3 newGravity)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (AnyModificationActive)
            {
                MarkValueModificationsDirty();
            }
        }

        public override Vector3 InterpolateValue(in Vector3 a, in Vector3 b, float t)
        {
            return ValueInterpolationFunctionType.Linear.Interpolate(a, b, t);
        }

        public override void UpdateValueModifications()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            GravityTracker.SetGravityUntracked(GetModifiedValue(GravityTracker.BaseGravity));
        }
    }
}
