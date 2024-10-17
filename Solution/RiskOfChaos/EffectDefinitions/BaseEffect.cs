using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions
{
    [Obsolete]
    public abstract class BaseEffect
    {
        [Obsolete]
        protected readonly Xoroshiro128Plus RNG = new Xoroshiro128Plus(0UL);

        [Obsolete]
        public virtual void OnPreStartServer()
        {
        }

        [Obsolete]
        public virtual void Serialize(NetworkWriter writer)
        {
        }

        [Obsolete]
        public virtual void Deserialize(NetworkReader reader)
        {
        }

        [Obsolete]
        public abstract void OnStart();
    }
}
