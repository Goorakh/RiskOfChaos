using RiskOfChaos.Networking.Wrappers;
using RoR2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.EffectComponents
{
    [DisallowMultipleComponent]
    public sealed class ChaosEffectComponent : NetworkBehaviour
    {
        public delegate void EffectDelegate(ChaosEffectComponent effectComponent);
        public static event EffectDelegate OnEffectStartGlobal;
        public static event EffectDelegate OnEffectEndGlobal;

        static readonly List<ChaosEffectComponent> _instances = [];
        public static readonly ReadOnlyCollection<ChaosEffectComponent> Instances = new ReadOnlyCollection<ChaosEffectComponent>(_instances);

        [SyncVar]
        int _chaosEffectIndexInternal;

        public ChaosEffectIndex ChaosEffectIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ChaosEffectIndex)(_chaosEffectIndexInternal - 1);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _chaosEffectIndexInternal = (int)value + 1;
        }

        public ChaosEffectInfo ChaosEffectInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ChaosEffectCatalog.GetEffectInfo(ChaosEffectIndex);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => ChaosEffectIndex = value?.EffectIndex ?? ChaosEffectIndex.Invalid;
        }

        [SyncVar]
        Net_RunFixedTimeStampWrapper _timeStarted;
        public Run.FixedTimeStamp TimeStarted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _timeStarted;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _timeStarted = value;
        }

        Xoroshiro128Plus _rng;
        public Xoroshiro128Plus Rng
        {
            [Server]
            get
            {
                if (_rng == null)
                {
                    Log.Error($"Effect {name} ({netId}) is missing RNG seed, generating random seed.");
                    SetRngSeedServer(RoR2Application.rng.nextUlong);
                }

                return _rng;
            }
        }

        [Server]
        public void SetRngSeedServer(ulong seed)
        {
#if DEBUG
            Log.Debug($"{name} ({netId}): Server RNG seed {seed}");
#endif

            _rng = new Xoroshiro128Plus(seed);
        }

        void Start()
        {
            if (ChaosEffectIndex == ChaosEffectIndex.Invalid)
            {
                Log.Error($"Effect controller {name} ({netId}) is missing an effect index");

                if (NetworkServer.active)
                {
                    RetireEffect();
                }

                return;
            }

            _instances.Add(this);
            OnEffectStartGlobal?.Invoke(this);
        }

        void OnDestroy()
        {
            if (_instances.Remove(this))
            {
                OnEffectEndGlobal?.Invoke(this);
            }
        }

        [Server]
        public void RetireEffect()
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}
