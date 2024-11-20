using HarmonyLib;
using MonoMod.RuntimeDetour;
using RoR2;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class GravityTracker
    {
        static Vector3 _baseGravity = Physics.gravity;
        public static Vector3 BaseGravity => _baseGravity;

        public static void SetGravityUntracked(Vector3 value)
        {
            _preventTrackingBaseGravity = true;
            Physics.gravity = value;
            _preventTrackingBaseGravity = false;
        }

        public static bool HasGravityAuthority => NetworkServer.active || NetworkServer.dontListen;

        public static void SetClientBaseGravity(Vector3 newBaseGravity)
        {
            if (HasGravityAuthority)
            {
                Log.Warning("Client sync called with gravity authority");
                return;
            }

#if DEBUG
            if (_baseGravity != newBaseGravity)
            {
                Log.Debug($"Updated client base gravity: {_baseGravity} -> {newBaseGravity}");
            }
#endif

            _baseGravity = newBaseGravity;
        }

        static bool _preventTrackingBaseGravity = false;
        static bool shouldTrackBaseGravity => !_preventTrackingBaseGravity && HasGravityAuthority;

        public delegate void OnBaseGravityChangedDelegate(Vector3 newGravity);

        public static event OnBaseGravityChangedDelegate OnBaseGravityChanged;

        [SystemInitializer]
        static void Init()
        {
            MethodInfo gravitySetter = AccessTools.DeclaredPropertySetter(typeof(Physics), nameof(Physics.gravity));
            if (gravitySetter != null)
            {
                new Hook(gravitySetter, On_Physics_set_gravity);
            }
            else
            {
                Log.Error("Could not find Physics.set_gravity method");
            }

            Run.onRunDestroyGlobal += onRunDestroyGlobal;
        }

        static void onRunDestroyGlobal(Run obj)
        {
            Log.Debug("Restoring base gravity");

            Vector3 baseGravity = new Vector3(0f, Run.baseGravity, 0f);
            Physics.gravity = baseGravity;
            _baseGravity = baseGravity;
        }

        delegate void orig_GravitySetter(Vector3 value);

        static void On_Physics_set_gravity(orig_GravitySetter orig, Vector3 value)
        {
            orig(value);

            tryTrackNewBaseGravity(ref _baseGravity, value, OnBaseGravityChanged);
        }

        static void tryTrackNewBaseGravity(ref Vector3 baseGravity, Vector3 newGravity, OnBaseGravityChangedDelegate baseGravityChangedCallback
#if DEBUG
            , [CallerMemberName] string caller = null
#endif
            )
        {
            if (!shouldTrackBaseGravity)
                return;

            if (baseGravity != newGravity)
            {
#if DEBUG
                string gravityType = caller.Substring(caller.LastIndexOf('_') + 1);
                Log.Debug($"base {gravityType}: {baseGravity} -> {newGravity}");
#endif

                baseGravity = newGravity;
                baseGravityChangedCallback?.Invoke(newGravity);
            }
        }
    }
}
