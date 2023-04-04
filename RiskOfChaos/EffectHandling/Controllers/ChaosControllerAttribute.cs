using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ChaosControllerAttribute : HG.Reflection.SearchableAttribute
    {
        public readonly bool ServerOnly;

        public ChaosControllerAttribute(bool serverOnly)
        {
            ServerOnly = serverOnly;
        }

        public virtual bool CanBeActive()
        {
            return !ServerOnly || NetworkServer.active;
        }
    }
}
