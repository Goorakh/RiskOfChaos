using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class OnDestroyEvent : MonoBehaviour
    {
        public delegate void OnDestroyedDelegate(GameObject obj);
        event OnDestroyedDelegate _onDestroyed;
        public event OnDestroyedDelegate OnDestroyed
        {
            add
            {
                _onDestroyed += value;

                Log.Debug($"Added OnDestroy event to {Util.GetGameObjectHierarchyName(gameObject)}");
            }
            remove
            {
                _onDestroyed -= value;

                Log.Debug($"Removed OnDestroy event from {Util.GetGameObjectHierarchyName(gameObject)}");
            }
        }

        void OnDestroy()
        {
            _onDestroyed?.Invoke(gameObject);
        }

        public static OnDestroyEvent Add(GameObject obj, OnDestroyedDelegate onDestroyed)
        {
            OnDestroyEvent onDestroyEvent = obj.EnsureComponent<OnDestroyEvent>();
            onDestroyEvent.OnDestroyed += onDestroyed;

            return onDestroyEvent;
        }

        public static void Remove(GameObject obj, OnDestroyedDelegate onDestroyed)
        {
            if (obj.TryGetComponent(out OnDestroyEvent onDestroyEvent))
            {
                onDestroyEvent.OnDestroyed -= onDestroyed;
            }
        }
    }
}
