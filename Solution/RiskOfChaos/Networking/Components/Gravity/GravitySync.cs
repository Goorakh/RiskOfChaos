using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.Patches;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components.Gravity
{
    public sealed class GravitySync : NetworkBehaviour
    {
        [ContentInitializer]
        static void LoadContent(ContentIntializerArgs args)
        {
            // GravityNetworker
            {
                GameObject prefab = Prefabs.CreateNetworkedPrefab("GravityNetworker", [
                    typeof(SetDontDestroyOnLoad),
                    typeof(DestroyOnRunEnd),
                    typeof(AutoCreateOnRunStart),
                    typeof(GravitySync)
                ]);

                args.ContentPack.networkedObjectPrefabs.Add([prefab]);
            }
        }

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

            if (!GravityTracker.HasGravityAuthority)
            {
                updateBaseGravity(_baseGravity);
                updateCurrentGravity(_currentGravity);
            }
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
            if (NetworkServer.active || NetworkClient.active)
            {
                Log.Debug("Restoring gravity");
                updateCurrentGravity(_baseGravity);
            }
            else
            {
                Log.Debug("Not restoring gravity, no network session active");
            }
        }

        void updateBaseGravity(Vector3 newBaseGravity)
        {
#if DEBUG
            if (_baseGravity != newBaseGravity)
            {
                Log.Debug($"Base gravity changed: {_baseGravity} -> {newBaseGravity}");
            }
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
            if (_currentGravity != newCurrentGravity)
            {
                Log.Debug($"Current gravity changed: {_currentGravity} -> {newCurrentGravity}");
            }
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
