using RiskOfChaos.Patches;
using UnityEngine;

namespace RiskOfChaos.Networking.Components.Gravity
{
    public class SyncWorldGravity : GenericSyncGravity
    {
        protected override Vector3 currentGravity => Physics.gravity;

        protected override void onGravityChanged(in Vector3 newGravity)
        {
            base.onGravityChanged(newGravity);

            if (GravityTracker.HasGravityAuthority)
            {
                GravityTracker.SetGravityUntracked(newGravity);
            }
            else
            {
                Physics.gravity = newGravity;
            }
        }
    }
}
