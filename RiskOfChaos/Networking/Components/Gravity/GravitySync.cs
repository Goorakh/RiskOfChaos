using RiskOfChaos.Patches;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components.Gravity
{
    public class GravitySync : NetworkBehaviour
    {
        [SyncVar(hook = nameof(updateBaseGravity))]
        Vector3 _baseGravity;

        [SyncVar(hook = nameof(updateCurrentGravity))]
        Vector3 _currentGravity;

        public override void OnStartServer()
        {
            base.OnStartServer();

            _baseGravity = GravityTracker.BaseGravity;
            _currentGravity = Physics.gravity;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            updateBaseGravity(_baseGravity);
            updateCurrentGravity(_currentGravity);
        }

        void FixedUpdate()
        {
            if (_baseGravity != GravityTracker.BaseGravity)
            {
                if (GravityTracker.HasGravityAuthority)
                {
                    _baseGravity = GravityTracker.BaseGravity;
                }
                else
                {
                    updateBaseGravity(_baseGravity);
                }
            }

            if (_currentGravity != Physics.gravity)
            {
                if (GravityTracker.HasGravityAuthority)
                {
                    _currentGravity = Physics.gravity;
                }
                else
                {
                    updateCurrentGravity(_currentGravity);
                }
            }
        }

        void OnDestroy()
        {
            updateBaseGravity(GravityTracker.BaseGravity);
            updateCurrentGravity(GravityTracker.BaseGravity);
        }

        void updateBaseGravity(Vector3 newBaseGravity)
        {
#if DEBUG
            Log.Debug($"Base gravity changed: {GravityTracker.BaseGravity} -> {newBaseGravity}");
#endif

            _baseGravity = newBaseGravity;

            if (!GravityTracker.HasGravityAuthority)
            {
                GravityTracker.SetClientBaseGravity(newBaseGravity);
            }
        }

        void updateCurrentGravity(Vector3 newCurrentGravity)
        {
#if DEBUG
            Log.Debug($"Current gravity changed: {Physics.gravity} -> {newCurrentGravity}");
#endif

            _currentGravity = newCurrentGravity;

            if (GravityTracker.HasGravityAuthority)
            {
                GravityTracker.SetGravityUntracked(newCurrentGravity);
            }
            else
            {
                Physics.gravity = newCurrentGravity;
            }
        }
    }
}
