using RiskOfChaos.Patches;
using RiskOfChaos.Utilities.Interpolation;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Gravity
{
    public class GravityModificationManager : NetworkedValueModificationManager<Vector3>
    {
        static GravityModificationManager _instance;
        public static GravityModificationManager Instance => _instance;

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
            GravityTracker.SetGravityUntracked(GetModifiedValue(GravityTracker.BaseGravity));
        }
    }
}
