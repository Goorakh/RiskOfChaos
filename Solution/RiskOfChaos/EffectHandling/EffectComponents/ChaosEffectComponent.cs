using RiskOfChaos.Networking.Wrappers;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.EffectComponents
{
    public sealed class ChaosEffectComponent : NetworkBehaviour
    {
        [SyncVar]
        public ChaosEffectIndex ChaosEffectIndex = ChaosEffectIndex.Invalid;

        public ChaosEffectInfo ChaosEffectInfo
        {
            get => ChaosEffectCatalog.GetEffectInfo(ChaosEffectIndex);
            set => ChaosEffectIndex = value?.EffectIndex ?? ChaosEffectIndex.Invalid;
        }

        [SyncVar]
        Net_RunFixedTimeStampWrapper _timeStarted;
        public Run.FixedTimeStamp TimeStarted
        {
            get => _timeStarted;
            set => _timeStarted = value;
        }

        Xoroshiro128Plus _rngServer;
        public Xoroshiro128Plus RngServer
        {
            [Server]
            get => _rngServer;
        }

        [Server]
        public void SetRngSeedServer(ulong seed)
        {
            _rngServer = new Xoroshiro128Plus(seed);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_rngServer == null)
            {
                Log.Error($"Effect {ChaosEffectInfo} ({name}) is missing RNG seed");
                SetRngSeedServer(0);
            }
        }

        void Start()
        {
            if (ChaosEffectIndex == ChaosEffectIndex.Invalid)
            {
                Log.Error($"Effect controller {name} is missing an effect index");
            }
        }
    }
}
