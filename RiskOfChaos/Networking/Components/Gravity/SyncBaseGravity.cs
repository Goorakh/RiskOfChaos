using RiskOfChaos.Patches;
using UnityEngine;

namespace RiskOfChaos.Networking.Components.Gravity
{
    public class SyncBaseGravity : GenericSyncGravity
    {
        protected override Vector3 currentGravity => GravityTracker.BaseGravity;

        protected override void onGravityChanged(in Vector3 newGravity)
        {
            base.onGravityChanged(newGravity);

            if (!GravityTracker.HasGravityAuthority)
            {
                GravityTracker.SetClientBaseGravity(newGravity);
            }
        }
    }
}
