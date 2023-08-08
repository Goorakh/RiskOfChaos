using RiskOfChaos.Patches;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Gravity
{
    public class GravityModificationManager : NetworkedValueModificationManager<IGravityModificationProvider, Vector3>
    {
        static GravityModificationManager _instance;
        public static GravityModificationManager Instance => _instance;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            if (NetworkServer.active)
            {
                GravityTracker.OnBaseGravityChanged += onBaseGravityChanged;
            }
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            GravityTracker.OnBaseGravityChanged -= onBaseGravityChanged;
        }

        void onBaseGravityChanged(Vector3 newGravity)
        {
            if (AnyModificationActive)
            {
                onModificationProviderDirty();
            }
        }

        protected override void updateValueModifications()
        {
            GravityTracker.SetGravityUntracked(getModifiedValue(GravityTracker.BaseGravity));
        }
    }
}
