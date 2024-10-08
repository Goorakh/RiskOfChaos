using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Networking.Wrappers;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
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

        public ChaosEffectIndex ChaosEffectIndex = ChaosEffectIndex.Invalid;

        public ChaosEffectInfo ChaosEffectInfo => ChaosEffectCatalog.GetEffectInfo(ChaosEffectIndex);

        [SyncVar]
        Net_RunFixedTimeStampWrapper _timeStarted;

        IEffectHUDVisibilityProvider[] _hudVisibilityProviders;

        [SerializedMember("rng")]
        Xoroshiro128Plus _rng;

        bool _isInitialized;

        [SerializedMember("ts")]
        public RunTimeStamp TimeStarted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Run.FixedTimeStamp)_timeStarted;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _timeStarted = (Run.FixedTimeStamp)value;
        }

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

        public bool ShouldDisplayOnHUD
        {
            get
            {
                if (!isActiveAndEnabled || ChaosEffectInfo == null)
                    return false;

                foreach (IEffectHUDVisibilityProvider hudVisibilityProvider in _hudVisibilityProviders)
                {
                    MonoBehaviour hudVisibilityProviderComponent = hudVisibilityProvider as MonoBehaviour;
                    if (hudVisibilityProviderComponent && hudVisibilityProviderComponent.isActiveAndEnabled && !hudVisibilityProvider.CanShowOnHUD)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public EffectNameFormatter EffectNameFormatter
        {
            get
            {
                if (!ChaosEffectNameFormattersNetworker.Instance)
                    return null;

                return ChaosEffectNameFormattersNetworker.Instance.GetNameFormatter(ChaosEffectIndex);
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

        void Awake()
        {
            _hudVisibilityProviders = GetComponents<IEffectHUDVisibilityProvider>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            tryInitialize();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            tryInitialize();
        }
        
        void tryInitialize()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

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
