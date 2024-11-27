using RiskOfChaos.Utilities.Extensions;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class OnDestroyEvent : MonoBehaviour
    {
        public delegate void OnDestroyedDelegate(GameObject obj);
        public event OnDestroyedDelegate OnDestroyed;

        void OnDestroy()
        {
            OnDestroyed?.Invoke(gameObject);
        }

        public static OnDestroyEvent Add(GameObject obj, OnDestroyedDelegate onDestroyed)
        {
            OnDestroyEvent onDestroyEvent = obj.EnsureComponent<OnDestroyEvent>();
            onDestroyEvent.OnDestroyed += onDestroyed;

            Log.Debug($"Added OnDestroy event to {obj}");

            return onDestroyEvent;
        }

        public static void Remove(GameObject obj, OnDestroyedDelegate onDestroyed)
        {
            if (obj.TryGetComponent(out OnDestroyEvent onDestroyEvent))
            {
                onDestroyEvent.OnDestroyed -= onDestroyed;

                Log.Debug($"Removed OnDestroy event from {obj}");
            }
        }
    }
}
