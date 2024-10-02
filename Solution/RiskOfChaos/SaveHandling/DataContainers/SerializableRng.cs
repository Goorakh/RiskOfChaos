using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers
{
    [Serializable]
    public class SerializableRng
    {
        [DataMember(Name = "s0")]
        public ulong state0;

        [DataMember(Name = "s1")]
        public ulong state1;

        public SerializableRng(Xoroshiro128Plus rng)
        {
            if (rng is null)
            {
                state0 = 0;
                state1 = 0;

                Log.Error($"{nameof(rng)} is null");
            }
            else
            {
                state0 = rng.state0;
                state1 = rng.state1;
            }
        }

        public Xoroshiro128Plus Construct()
        {
            return new Xoroshiro128Plus(0UL)
            {
                state0 = state0,
                state1 = state1
            };
        }

        public static implicit operator Xoroshiro128Plus(SerializableRng serializable)
        {
            return serializable.Construct();
        }
    }
}