using System;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ChaosControllerAttribute : HG.Reflection.SearchableAttribute
    {
        public readonly bool ServerOnly;

        public event Action OnShouldRefreshEnabledState;

        public ChaosControllerAttribute(bool serverOnly)
        {
            ServerOnly = serverOnly;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void invokeShouldRefreshEnabledState()
        {
            OnShouldRefreshEnabledState?.Invoke();
        }

        public virtual bool CanBeActive()
        {
            return !ServerOnly || NetworkServer.active;
        }
    }
}
