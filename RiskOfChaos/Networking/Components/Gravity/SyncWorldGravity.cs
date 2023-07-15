using Newtonsoft.Json.Linq;
using RiskOfChaos.Patches;
using RoR2;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

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
