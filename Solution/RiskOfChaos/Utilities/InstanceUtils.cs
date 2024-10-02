using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class InstanceUtils
    {
        public static void DestroyAllTrackedInstances<T>(bool destroyGameObject = false) where T : MonoBehaviour
        {
            List<T> instancesList = InstanceTracker.GetInstancesList<T>();
            for (int i = instancesList.Count - 1; i >= 0; i--)
            {
                T obj = instancesList[i];
                if (obj)
                {
                    GameObject.Destroy(destroyGameObject ? obj.gameObject : obj);
                }
            }
        }
    }
}
