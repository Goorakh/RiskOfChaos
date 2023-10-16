using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;

using UnityObject = UnityEngine.Object;

namespace RiskOfChaos.Utilities
{
    public static class ObjectCleanup
    {
        [Flags]
        enum CleanupFlags : byte
        {
            None = 0,
            RunEnd = 1 << 0,
            StageEnd = 1 << 1
        }

        readonly record struct TrackedObject(UnityObject Obj, CleanupFlags Flags);
        static readonly List<TrackedObject> _trackedObjects = new List<TrackedObject>();

        static void runCleanup(CleanupFlags flags)
        {
#if DEBUG
            int numObjectsDestroyed = 0;
#endif

            for (int i = _trackedObjects.Count - 1; i >= 0; i--)
            {
                if (!_trackedObjects[i].Obj)
                {
                    _trackedObjects.RemoveAt(i);
                }
                else if ((_trackedObjects[i].Flags & flags) != 0)
                {
                    UnityObject.Destroy(_trackedObjects[i].Obj);
                    _trackedObjects.RemoveAt(i);

#if DEBUG
                    numObjectsDestroyed++;
#endif
                }
            }

#if DEBUG
            if (numObjectsDestroyed > 0)
            {
                Log.Debug($"Cleaned up {numObjectsDestroyed} object(s), CleanupFlags={flags}");
            }
#endif
        }

        static ObjectCleanup()
        {
            Run.onRunDestroyGlobal += _ =>
            {
                runCleanup(CleanupFlags.RunEnd | CleanupFlags.StageEnd);
            };

            Stage.onServerStageComplete += _ =>
            {
                runCleanup(CleanupFlags.StageEnd);
            };
        }

        public static void OnRunEnd(UnityObject obj)
        {
            _trackedObjects.Add(new TrackedObject(obj, CleanupFlags.RunEnd));
        }

        public static void OnStageEnd(UnityObject obj)
        {
            if (NetworkServer.active)
            {
                _trackedObjects.Add(new TrackedObject(obj, CleanupFlags.StageEnd));
            }
            else
            {
                Log.Warning($"Attempting to register StageEnd cleanup for {obj} as client, this is not supported, registering for RunEnd cleanup instead");
                OnRunEnd(obj);
            }
        }
    }
}
