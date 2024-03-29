﻿using RiskOfChaos.EffectHandling;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions
{
    public abstract class BaseEffect
    {
        bool _isInitialized;

        public ulong DispatchID { get; private set; }
        ulong _rngSeed;

        protected readonly Xoroshiro128Plus RNG = new Xoroshiro128Plus(0UL);

        public readonly ChaosEffectInfo EffectInfo;

        public BaseEffect()
        {
            EffectInfo = ChaosEffectCatalog.FindEffectInfoByType(GetType());
        }

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

            DispatchID = args.DispatchID;
            _rngSeed = args.RNGSeed;

            _isInitialized = true;
        }

        public virtual void OnPreStartServer()
        {
            initializeRNG();
        }

        public virtual void Serialize(NetworkWriter writer)
        {
            writer.WritePackedUInt64(DispatchID);
            writer.WritePackedUInt64(_rngSeed);
        }

        public virtual void Deserialize(NetworkReader reader)
        {
            DispatchID = reader.ReadPackedUInt64();

            _rngSeed = reader.ReadPackedUInt64();
            initializeRNG();
        }

        public abstract void OnStart();
    }
}
