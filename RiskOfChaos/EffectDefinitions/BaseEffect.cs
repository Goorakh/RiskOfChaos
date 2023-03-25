using RiskOfChaos.EffectHandling;
using RiskOfOptions.Options;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions
{
    public abstract class BaseEffect
    {
        bool _isInitialized;

        protected Xoroshiro128Plus RNG;

        public void Initialize(Xoroshiro128Plus rng)
        {
            if (_isInitialized)
            {
                Log.Warning($"Attempting to initialize already initialized effect class {this}");
                return;
            }

            RNG = rng;

            _isInitialized = true;
        }

        public virtual void Serialize(NetworkWriter writer)
        {
        }

        public virtual void Deserialize(NetworkReader reader)
        {
        }

        public abstract void OnStart();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void addConfigOption(BaseOption baseOption)
        {
            ChaosEffectCatalog.AddEffectConfigOption(baseOption);
        }
    }
}
