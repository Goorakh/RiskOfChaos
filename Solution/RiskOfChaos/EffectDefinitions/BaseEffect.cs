using RiskOfChaos.EffectHandling;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions
{
    [Obsolete]
    public abstract class BaseEffect
    {
        public ulong DispatchID { get; private set; }

        protected readonly Xoroshiro128Plus RNG = new Xoroshiro128Plus(0UL);

        public readonly ChaosEffectInfo EffectInfo;

        public virtual void OnPreStartServer()
        {
        }

        public virtual void Serialize(NetworkWriter writer)
        {
        }

        public virtual void Deserialize(NetworkReader reader)
        {
        }

        public abstract void OnStart();
    }
}
