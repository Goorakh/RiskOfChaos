using RiskOfChaos.EffectHandling;
using RiskOfOptions.Options;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions
{
    public abstract class BaseEffect
    {
        bool _isInitialized;

        ulong _rngSeed;

        protected readonly Xoroshiro128Plus RNG = new Xoroshiro128Plus(0UL);

        void initializeRNG()
        {
            RNG.ResetSeed(_rngSeed);
        }

        public void Initialize(in CreateEffectInstanceArgs args)
        {
            if (_isInitialized)
            {
                Log.Warning($"Attempting to initialize already initialized effect class {this}");
                return;
            }

            _rngSeed = args.RNGSeed;

            _isInitialized = true;
        }

        public virtual void OnPreStartServer()
        {
            initializeRNG();
        }

        public virtual void Serialize(NetworkWriter writer)
        {
            writer.WritePackedUInt64(_rngSeed);
        }

        public virtual void Deserialize(NetworkReader reader)
        {
            _rngSeed = reader.ReadPackedUInt64();
            initializeRNG();
        }

        public abstract void OnStart();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void addConfigOption(BaseOption baseOption)
        {
            ChaosEffectCatalog.AddEffectConfigOption(baseOption);
        }
    }
}
